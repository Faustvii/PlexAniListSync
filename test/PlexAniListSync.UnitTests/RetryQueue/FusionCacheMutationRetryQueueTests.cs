using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.RetryQueue;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace PlexAniListSync.UnitTests.RetryQueue;

public class FusionCacheMutationRetryQueueTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static FusionCacheMutationRetryQueue BuildQueue() =>
        new(new FusionCache(Options.Create(new FusionCacheOptions())));

    private static WebhookData Webhook(string user, string guid, int season, int episode) =>
        new()
        {
            User = user,
            PlexGuid = guid,
            Season = season,
            Episode = episode,
            ShowTitle = "Some Show",
            Type = MediaType.Show,
        };

    [Fact]
    public async Task Enqueue_ThenDequeueDue_ReturnsTheEntry()
    {
        var queue = BuildQueue();
        await queue.EnqueueAsync(Webhook("u", "g", 1, 5), Now, CancellationToken.None);

        var due = await queue.DequeueDueAsync(Now, limit: 10, CancellationToken.None);

        var entry = Assert.Single(due);
        Assert.Equal(5, entry.Data.Episode);
        Assert.Equal(0, entry.Attempts);
    }

    [Fact]
    public async Task DequeueDue_ExcludesEntriesScheduledInTheFuture()
    {
        var queue = BuildQueue();
        var entry = await EnqueueAndGet(queue, Webhook("u", "g", 1, 5));
        await queue.RescheduleAsync(entry, Now.AddHours(1), CancellationToken.None);

        Assert.Empty(await queue.DequeueDueAsync(Now, 10, CancellationToken.None));
        Assert.Single(await queue.DequeueDueAsync(Now.AddHours(2), 10, CancellationToken.None));
    }

    [Fact]
    public async Task Enqueue_SameEpisodeTwice_IsOneEntryWithResetBackoff()
    {
        var queue = BuildQueue();
        var first = await EnqueueAndGet(queue, Webhook("u", "g", 1, 5));
        await queue.RescheduleAsync(first, Now.AddHours(1), CancellationToken.None);

        await queue.EnqueueAsync(Webhook("u", "g", 1, 5), Now, CancellationToken.None);

        var due = await queue.DequeueDueAsync(Now, 10, CancellationToken.None);
        var entry = Assert.Single(due);
        Assert.Equal(0, entry.Attempts);
    }

    [Fact]
    public async Task DistinctEpisodes_AreSeparateEntries()
    {
        var queue = BuildQueue();
        await queue.EnqueueAsync(Webhook("u", "g", 1, 5), Now, CancellationToken.None);
        await queue.EnqueueAsync(Webhook("u", "g", 1, 6), Now, CancellationToken.None);

        var due = await queue.DequeueDueAsync(Now, 10, CancellationToken.None);

        Assert.Equal(new[] { 5, 6 }, due.Select(e => e.Data.Episode).OrderBy(e => e));
    }

    [Fact]
    public async Task Reschedule_BumpsAttemptsAndPushesNextAttempt()
    {
        var queue = BuildQueue();
        var entry = await EnqueueAndGet(queue, Webhook("u", "g", 1, 5));

        await queue.RescheduleAsync(entry, Now.AddMinutes(5), CancellationToken.None);

        var rescheduled = Assert.Single(await queue.DequeueDueAsync(Now.AddMinutes(10), 10, CancellationToken.None));
        Assert.Equal(1, rescheduled.Attempts);
        Assert.Equal(Now.AddMinutes(5), rescheduled.NextAttemptAt);
    }

    [Fact]
    public async Task Remove_DeletesTheEntry()
    {
        var queue = BuildQueue();
        var entry = await EnqueueAndGet(queue, Webhook("u", "g", 1, 5));

        await queue.RemoveAsync(entry, CancellationToken.None);

        Assert.Empty(await queue.DequeueDueAsync(Now.AddDays(1), 10, CancellationToken.None));
    }

    [Fact]
    public async Task DequeueDue_RespectsTheLimit()
    {
        var queue = BuildQueue();
        for (var episode = 1; episode <= 5; episode++)
            await queue.EnqueueAsync(Webhook("u", "g", 1, episode), Now, CancellationToken.None);

        var due = await queue.DequeueDueAsync(Now, limit: 2, CancellationToken.None);

        Assert.Equal(2, due.Count);
    }

    private static async Task<QueuedMutation> EnqueueAndGet(FusionCacheMutationRetryQueue queue, WebhookData data)
    {
        await queue.EnqueueAsync(data, Now, CancellationToken.None);
        var due = await queue.DequeueDueAsync(Now, 100, CancellationToken.None);
        return due.Single(e => e.Data.Episode == data.Episode && e.Data.PlexGuid == data.PlexGuid);
    }
}
