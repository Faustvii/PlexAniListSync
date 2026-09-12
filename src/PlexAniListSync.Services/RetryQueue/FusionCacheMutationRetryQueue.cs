using PlexAniListSync.Models.Webhook;
using ZiggyCreatures.Caching.Fusion;

namespace PlexAniListSync.Services.RetryQueue;

public sealed class FusionCacheMutationRetryQueue : IMutationRetryQueue
{
    private const string QueueKey = "anilist:retry-queue";

    private static readonly TimeSpan NeverExpire = TimeSpan.MaxValue;

    private readonly IFusionCache _cache;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FusionCacheMutationRetryQueue(IFusionCache cache) => _cache = cache;

    public async Task<bool> EnqueueAsync(WebhookData data, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAsync();
            var key = KeyOf(data);
            entries.RemoveAll(e => string.Equals(KeyOf(e.Data), key, StringComparison.Ordinal));
            entries.Add(new QueuedMutation(data, now, Attempts: 0, NextAttemptAt: now));
            await WriteAsync(entries);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<QueuedMutation>> DequeueDueAsync(
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAsync();
            return entries
                .Where(e => e.NextAttemptAt <= now)
                .OrderBy(e => e.EnqueuedAt)
                .Take(limit)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveAsync(QueuedMutation entry, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAsync();
            var key = KeyOf(entry.Data);
            if (entries.RemoveAll(e => string.Equals(KeyOf(e.Data), key, StringComparison.Ordinal)) > 0)
                await WriteAsync(entries);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RescheduleAsync(QueuedMutation entry, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadAsync();
            var key = KeyOf(entry.Data);
            var index = entries.FindIndex(e => string.Equals(KeyOf(e.Data), key, StringComparison.Ordinal));
            if (index < 0)
                return;

            entries[index] = entries[index] with
            {
                Attempts = entries[index].Attempts + 1,
                NextAttemptAt = nextAttemptAt,
            };
            await WriteAsync(entries);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<QueuedMutation>> ReadAsync() =>
        await _cache.GetOrDefaultAsync<List<QueuedMutation>>(QueueKey) ?? new List<QueuedMutation>();

    private ValueTask WriteAsync(List<QueuedMutation> entries) =>
        _cache.SetAsync(QueueKey, entries, options => options.SetDuration(NeverExpire).SetFailSafe(false));

    private static string KeyOf(WebhookData data) =>
        string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{data.User}|{data.PlexGuid}|{data.Season}|{data.Episode}"
        );
}
