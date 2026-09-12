using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlexAniListSync.AniListNet;
using PlexAniListSync.Models.RetryQueue;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.Mappings;
using PlexAniListSync.Services.RetryQueue;
using PlexAniListSync.Services.Webhook;
using Xunit;

namespace PlexAniListSync.UnitTests.Webhook;

public class WebhookServiceRetryTests
{
    private const int AnilistId = 100;

    private static WebhookData Webhook() =>
        new()
        {
            User = "plexuser",
            PlexGuid = "plex://show/1",
            Season = 1,
            Episode = 5,
            ShowTitle = "Some Show",
            Type = MediaType.Show,
        };

    private static AniListRateLimitException RateLimit() =>
        new("req", "body", retryAfter: 30, rateResetUnixSeconds: null);

    private static WebhookService BuildService(
        Mock<IAniListService> aniList,
        Mock<IMutationRetryQueue> queue,
        IRateLimitGate gate,
        Mock<IMappingService>? mapping = null
    )
    {
        mapping ??= DefaultMapping();
        return new WebhookService(
            Mock.Of<ILogger<WebhookService>>(),
            mapping.Object,
            aniList.Object,
            queue.Object,
            gate,
            Options.Create(new RetryQueueOptions())
        );
    }

    private static Mock<IMappingService> DefaultMapping()
    {
        var mapping = new Mock<IMappingService>();
        mapping.Setup(x => x.GetAniListIdFromPlexGuid(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(AnilistId);
        mapping.Setup(x => x.GetEpisodeNumber(It.IsAny<int>(), AnilistId)).Returns((5, AnilistId));
        return mapping;
    }

    [Fact]
    public async Task HappyPath_UpdatesAndDoesNotQueue()
    {
        var aniList = new Mock<IAniListService>();
        var queue = new Mock<IMutationRetryQueue>(MockBehavior.Strict);
        var service = BuildService(aniList, queue, new RateLimitGate());

        var result = await service.HandleAsync(Webhook());

        Assert.Equal(WebhookResult.Applied, result);
        aniList.Verify(x => x.UpdateMediaAsync("plexuser", AnilistId, 5, MediaType.Show), Times.Once);
        queue.Verify(
            x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task RateLimited_QueuesPausesGateAndStillAccepts()
    {
        var aniList = new Mock<IAniListService>();
        aniList
            .Setup(x => x.UpdateMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MediaType>()))
            .ThrowsAsync(RateLimit());
        var queue = new Mock<IMutationRetryQueue>();
        queue
            .Setup(x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var gate = new RateLimitGate();
        var service = BuildService(aniList, queue, gate);

        var result = await service.HandleAsync(Webhook());

        Assert.Equal(WebhookResult.Queued, result);
        Assert.True(gate.IsPaused(DateTimeOffset.UtcNow));
        queue.Verify(
            x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RateLimited_WhenQueueDisabled_SurfacesAsRateLimitedError()
    {
        var aniList = new Mock<IAniListService>();
        aniList
            .Setup(x => x.UpdateMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MediaType>()))
            .ThrowsAsync(RateLimit());
        var queue = new Mock<IMutationRetryQueue>();
        queue
            .Setup(x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var service = BuildService(aniList, queue, new RateLimitGate());

        var result = await service.HandleAsync(Webhook());

        Assert.Equal(WebhookResult.RateLimited, result);
    }

    [Fact]
    public async Task GateAlreadyPaused_QueuesWithoutTouchingAniList()
    {
        var aniList = new Mock<IAniListService>(MockBehavior.Strict);
        var queue = new Mock<IMutationRetryQueue>();
        queue
            .Setup(x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var mapping = new Mock<IMappingService>(MockBehavior.Strict);
        var gate = new RateLimitGate();
        gate.Pause(DateTimeOffset.UtcNow.AddMinutes(5));
        var service = BuildService(aniList, queue, gate, mapping);

        var result = await service.HandleAsync(Webhook());

        Assert.Equal(WebhookResult.Queued, result);
        queue.Verify(
            x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Unresolvable_ReturnsFalseAndDoesNotQueue()
    {
        var mapping = new Mock<IMappingService>();
        mapping.Setup(x => x.GetAniListIdFromPlexGuid(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        var aniList = new Mock<IAniListService>();
        aniList.Setup(x => x.FindShowAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync((int?)null);
        var queue = new Mock<IMutationRetryQueue>(MockBehavior.Strict);
        var service = BuildService(aniList, queue, new RateLimitGate(), mapping);

        var result = await service.HandleAsync(Webhook());

        Assert.Equal(WebhookResult.NotFound, result);
        queue.Verify(
            x => x.EnqueueAsync(It.IsAny<WebhookData>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
