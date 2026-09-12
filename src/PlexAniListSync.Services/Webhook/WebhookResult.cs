namespace PlexAniListSync.Services.Webhook;

public enum WebhookResult
{
    /// <summary>The mutation was applied (or skipped as a no-op). HTTP 200.</summary>
    Applied,

    /// <summary>AniList was rate-limiting; the webhook was durably queued for a later retry. HTTP 202.</summary>
    Queued,

    /// <summary>
    /// AniList was rate-limiting but the retry queue is disabled, so the mutation could not be queued.
    /// The 429 is surfaced to the caller as an error. HTTP 5xx.
    /// </summary>
    RateLimited,

    /// <summary>The show could not be resolved to an AniList id. HTTP 400.</summary>
    NotFound,
}
