using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.RetryQueue;

/// <summary>
/// Durable store of AniList mutations that must be retried after a 429. The natural identity of an
/// entry is <c>(plex user, plex guid, season, episode)</c>; enqueuing the same watch event again is
/// an idempotent upsert that resets the backoff. Cross-entry coalescing (many watch events resolving
/// to one AniList id) is handled at drain time, not here.
/// </summary>
public interface IMutationRetryQueue
{
    /// <summary>
    /// Upsert a failed webhook, scheduling its first retry shortly after <paramref name="now"/>.
    /// Returns <c>true</c> if it was durably queued, <c>false</c> if the queue is disabled (so the
    /// caller can surface the 429 as an error instead of pretending it was accepted).
    /// </summary>
    Task<bool> EnqueueAsync(WebhookData data, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Entries whose <c>NextAttemptAt</c> is at or before <paramref name="now"/>, oldest first, capped at <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<QueuedMutation>> DequeueDueAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);

    /// <summary>Remove an entry (it succeeded, expired, or became unresolvable).</summary>
    Task RemoveAsync(QueuedMutation entry, CancellationToken cancellationToken);

    /// <summary>Bump the attempt count and push the next retry to <paramref name="nextAttemptAt"/>.</summary>
    Task RescheduleAsync(QueuedMutation entry, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken);
}
