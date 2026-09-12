namespace PlexAniListSync.Models.RetryQueue;

/// <summary>
/// Configuration for the durable 429 retry queue that re-attempts AniList mutations which
/// failed because AniList was rate-limiting (HTTP 429).
/// </summary>
public class RetryQueueOptions
{
    public const string Key = "RetryQueue";

    /// <summary>
    /// When false, nothing is persisted and the drain service does not run; a 429 on the live webhook
    /// path is then logged and dropped (the pre-queue behaviour) instead of queued.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often the drain service wakes to re-attempt due entries.</summary>
    public int DrainIntervalSeconds { get; set; } = 300;

    /// <summary>Maximum number of entries pulled per drain cycle.</summary>
    public int BatchSize { get; set; } = 200;

    /// <summary>First backoff step; subsequent attempts double it up to <see cref="MaxBackoffSeconds"/>.</summary>
    public int BaseBackoffSeconds { get; set; } = 300;

    /// <summary>Upper bound on a single backoff step.</summary>
    public int MaxBackoffSeconds { get; set; } = 7200;

    /// <summary>How long an entry may live before it is dropped as stale.</summary>
    public int TtlHours { get; set; } = 24;

    /// <summary>Fallback pause applied when a 429 carries neither <c>Retry-After</c> nor a reset header.</summary>
    public int DefaultPauseSeconds { get; set; } = 60;
}
