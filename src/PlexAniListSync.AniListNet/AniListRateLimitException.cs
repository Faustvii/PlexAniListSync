using System.Net;
using PlexAniListSync.AniListNet.Helpers;

namespace PlexAniListSync.AniListNet;

/// <summary>
/// Thrown when AniList responds with HTTP 429.
/// </summary>
public sealed class AniListRateLimitException : AniException
{
    /// <summary>Value of the <c>Retry-After</c> header (seconds), when present.</summary>
    public int? RetryAfter { get; }

    /// <summary>When the rate limit resets, derived from <c>X-RateLimit-Reset</c> or <c>Retry-After</c>.</summary>
    public DateTime? RateReset { get; }

    internal AniListRateLimitException(string actualRequestBody, string actualResponseBody, int? retryAfter,
        int? rateResetUnixSeconds)
        : base("AniList rate limit exceeded (HTTP 429).", actualRequestBody, actualResponseBody,
            HttpStatusCode.TooManyRequests)
    {
        RetryAfter = retryAfter;
        RateReset = HelperUtilities.ResolveRateReset(rateResetUnixSeconds, retryAfter);
    }
}
