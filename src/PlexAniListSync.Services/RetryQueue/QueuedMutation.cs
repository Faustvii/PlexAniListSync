using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.RetryQueue;

public sealed record QueuedMutation(
    WebhookData Data,
    DateTimeOffset EnqueuedAt,
    int Attempts,
    DateTimeOffset NextAttemptAt
);
