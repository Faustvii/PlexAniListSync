using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlexAniListSync.AniListNet;
using PlexAniListSync.Models.RetryQueue;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.HostedServices;
using PlexAniListSync.Services.RetryQueue;
using PlexAniListSync.Services.Webhook;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace PlexAniListSync.UnitTests.RetryQueue;

public class MutationRetryDrainServiceTests
{
    private const string User = "plexuser";

    private static WebhookData Webhook(int episode, string guid = "plex://show/1") =>
        new()
        {
            User = User,
            PlexGuid = guid,
            Season = 1,
            Episode = episode,
            ShowTitle = "Some Show",
            Type = MediaType.Show,
        };

    private static AniListRateLimitException RateLimit() =>
        new("req", "body", retryAfter: 30, rateResetUnixSeconds: null);

    private sealed class Harness
    {
        public Mock<IWebhookService> Webhook { get; } = new();
        public Mock<IAniListService> AniList { get; } = new();
        public IMutationRetryQueue Queue { get; } = new FusionCacheMutationRetryQueue(
            new FusionCache(Options.Create(new FusionCacheOptions()))
        );
        public RateLimitGate Gate { get; } = new();

        public MutationRetryDrainService Build(RetryQueueOptions? options = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(Webhook.Object);
            services.AddSingleton(AniList.Object);
            var provider = services.BuildServiceProvider();

            return new MutationRetryDrainService(
                Mock.Of<ILogger<MutationRetryDrainService>>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                Queue,
                Gate,
                Options.Create(options ?? new RetryQueueOptions())
            );
        }

        public void ResolveToSameId(int anilistId) =>
            Webhook
                .Setup(x => x.ResolveAsync(It.IsAny<WebhookData>()))
                .ReturnsAsync((WebhookData d) => new ResolvedMutation(anilistId, d.Episode, d.Type));
    }

    [Fact]
    public async Task Drain_CoalescesEntriesForOneAniListIdToTheMaxEpisode()
    {
        var h = new Harness();
        h.ResolveToSameId(500);
        var enqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await h.Queue.EnqueueAsync(Webhook(5), enqueuedAt, CancellationToken.None);
        await h.Queue.EnqueueAsync(Webhook(6), enqueuedAt, CancellationToken.None);
        var service = h.Build();

        await service.DrainAsync(CancellationToken.None);

        h.AniList.Verify(x => x.UpdateMediaAsync(User, 500, 6, MediaType.Show), Times.Once);
        h.AniList.Verify(
            x => x.UpdateMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MediaType>()),
            Times.Once
        );
        Assert.Empty(await h.Queue.DequeueDueAsync(DateTimeOffset.UtcNow.AddDays(1), 10, CancellationToken.None));
    }

    [Fact]
    public async Task Drain_OnRateLimit_PausesGateAndReschedulesInsteadOfRemoving()
    {
        var h = new Harness();
        h.ResolveToSameId(500);
        h.AniList
            .Setup(x => x.UpdateMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MediaType>()))
            .ThrowsAsync(RateLimit());
        await h.Queue.EnqueueAsync(Webhook(5), DateTimeOffset.UtcNow.AddMinutes(-1), CancellationToken.None);
        var service = h.Build();

        await service.DrainAsync(CancellationToken.None);

        Assert.True(h.Gate.IsPaused(DateTimeOffset.UtcNow));
        Assert.Empty(await h.Queue.DequeueDueAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));
        var later = await h.Queue.DequeueDueAsync(DateTimeOffset.UtcNow.AddDays(1), 10, CancellationToken.None);
        Assert.Equal(1, Assert.Single(later).Attempts);
    }

    [Fact]
    public async Task Drain_WhenGatePaused_DoesNothing()
    {
        var h = new Harness();
        h.Gate.Pause(DateTimeOffset.UtcNow.AddMinutes(5));
        await h.Queue.EnqueueAsync(Webhook(5), DateTimeOffset.UtcNow.AddMinutes(-1), CancellationToken.None);
        var service = h.Build();

        await service.DrainAsync(CancellationToken.None);

        h.Webhook.Verify(x => x.ResolveAsync(It.IsAny<WebhookData>()), Times.Never);
        h.AniList.Verify(
            x => x.UpdateMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MediaType>()),
            Times.Never
        );
        Assert.Single(await h.Queue.DequeueDueAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));
    }

    [Fact]
    public async Task Drain_DropsEntriesPastTtlWithoutResolving()
    {
        var h = new Harness();
        await h.Queue.EnqueueAsync(Webhook(5), DateTimeOffset.UtcNow.AddHours(-25), CancellationToken.None);
        var service = h.Build(new RetryQueueOptions { TtlHours = 24 });

        await service.DrainAsync(CancellationToken.None);

        h.Webhook.Verify(x => x.ResolveAsync(It.IsAny<WebhookData>()), Times.Never);
        Assert.Empty(await h.Queue.DequeueDueAsync(DateTimeOffset.UtcNow.AddDays(2), 10, CancellationToken.None));
    }

    [Fact]
    public async Task Drain_DropsUnresolvableEntries()
    {
        var h = new Harness();
        h.Webhook.Setup(x => x.ResolveAsync(It.IsAny<WebhookData>())).ReturnsAsync((ResolvedMutation?)null);
        await h.Queue.EnqueueAsync(Webhook(5), DateTimeOffset.UtcNow.AddMinutes(-1), CancellationToken.None);
        var service = h.Build();

        await service.DrainAsync(CancellationToken.None);

        h.AniList.Verify(
            x => x.UpdateMediaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MediaType>()),
            Times.Never
        );
        Assert.Empty(await h.Queue.DequeueDueAsync(DateTimeOffset.UtcNow.AddDays(1), 10, CancellationToken.None));
    }

    [Fact]
    public async Task Drain_KeepsDistinctAniListIdsSeparate()
    {
        // A Plex season split across two AniList ids (cour split) must produce two mutations, not one.
        var h = new Harness();
        h.Webhook
            .Setup(x => x.ResolveAsync(It.IsAny<WebhookData>()))
            .ReturnsAsync((WebhookData d) => new ResolvedMutation(d.Episode <= 12 ? 100 : 200, d.Episode, d.Type));
        var enqueuedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await h.Queue.EnqueueAsync(Webhook(12), enqueuedAt, CancellationToken.None);
        await h.Queue.EnqueueAsync(Webhook(13), enqueuedAt, CancellationToken.None);
        var service = h.Build();

        await service.DrainAsync(CancellationToken.None);

        h.AniList.Verify(x => x.UpdateMediaAsync(User, 100, 12, MediaType.Show), Times.Once);
        h.AniList.Verify(x => x.UpdateMediaAsync(User, 200, 13, MediaType.Show), Times.Once);
    }
}
