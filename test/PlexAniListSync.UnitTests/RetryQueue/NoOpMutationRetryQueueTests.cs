using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.RetryQueue;
using Xunit;

namespace PlexAniListSync.UnitTests.RetryQueue;

public class NoOpMutationRetryQueueTests
{
    [Fact]
    public async Task Enqueue_ReturnsFalse_SoTheCallerSurfacesTheRateLimit()
    {
        var queue = new NoOpMutationRetryQueue(NullLogger<NoOpMutationRetryQueue>.Instance);
        var data = new WebhookData { User = "u", PlexGuid = "g", Season = 1, Episode = 5 };

        var queued = await queue.EnqueueAsync(data, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.False(queued);
    }

    [Fact]
    public async Task DequeueDue_IsAlwaysEmpty()
    {
        var queue = new NoOpMutationRetryQueue(NullLogger<NoOpMutationRetryQueue>.Instance);

        Assert.Empty(await queue.DequeueDueAsync(DateTimeOffset.UtcNow, 10, CancellationToken.None));
    }
}
