using PlexAniListSync.AniListNet;
using PlexAniListSync.Models.RetryQueue;

namespace PlexAniListSync.Services.RetryQueue;

public static class RetryScheduling
{
    /// <summary>
    /// When an entry on its <paramref name="attempts"/>-th failure should next be attempted.
    /// Exponential from <see cref="RetryQueueOptions.BaseBackoffSeconds"/>, capped at
    /// <see cref="RetryQueueOptions.MaxBackoffSeconds"/>, never earlier than <paramref name="notBefore"/>
    /// (the rate-limit reset, when known).
    /// </summary>
    public static DateTimeOffset NextAttempt(
        int attempts,
        DateTimeOffset now,
        RetryQueueOptions options,
        DateTimeOffset? notBefore
    )
    {
        var steps = Math.Max(0, attempts - 1);
        // Clamp the exponent so 2^steps cannot overflow a double into infinity on a stuck entry.
        var factor = steps >= 31 ? double.MaxValue : Math.Pow(2, steps);
        var seconds = Math.Min(options.MaxBackoffSeconds, options.BaseBackoffSeconds * factor);
        var next = now.AddSeconds(seconds);
        if (notBefore is { } floor && floor > next)
            next = floor;
        return next;
    }

    /// <summary>True once an entry has lived longer than <see cref="RetryQueueOptions.TtlHours"/>.</summary>
    public static bool IsExpired(DateTimeOffset enqueuedAt, DateTimeOffset now, RetryQueueOptions options) =>
        now - enqueuedAt > TimeSpan.FromHours(options.TtlHours);

    /// <summary>
    /// The instant a 429 should pause AniList traffic until: the rate-limit reset if present, else
    /// <c>Retry-After</c> seconds from now, else a small default pause.
    /// </summary>
    public static DateTimeOffset PauseUntil(AniListRateLimitException exception, DateTimeOffset now, RetryQueueOptions options)
    {
        if (exception.RateReset is { } reset)
        {
            var resetUtc = DateTime.SpecifyKind(reset, DateTimeKind.Utc);
            var candidate = new DateTimeOffset(resetUtc);
            if (candidate > now)
                return candidate;
        }

        if (exception.RetryAfter is > 0)
            return now.AddSeconds(exception.RetryAfter.Value);

        return now.AddSeconds(options.DefaultPauseSeconds);
    }
}
