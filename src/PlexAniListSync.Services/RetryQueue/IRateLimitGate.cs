namespace PlexAniListSync.Services.RetryQueue;

/// <summary>
/// A single process-wide "paused-until" gate. When AniList returns a 429 we record when the limit is
/// expected to reset so neither the drain loop nor the live webhook path keeps hammering AniList in
/// the meantime.
/// </summary>
public interface IRateLimitGate
{
    /// <summary>The instant the current pause lifts, or null when not paused.</summary>
    DateTimeOffset? PausedUntil { get; }

    /// <summary>True while <paramref name="now"/> is before the recorded reset instant.</summary>
    bool IsPaused(DateTimeOffset now);

    /// <summary>Record a pause until <paramref name="until"/>. A later (further-out) reset wins over an earlier one.</summary>
    void Pause(DateTimeOffset until);
}
