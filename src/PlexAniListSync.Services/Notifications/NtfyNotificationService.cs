using Microsoft.Extensions.Options;
using PlexAniListSync.Models.Notifications;
using PlexAniListSync.Models.Webhook;
using static PlexAniListSync.Models.AniList.AniListOptions;

namespace PlexAniListSync.Services.Notifications;

internal sealed class NtfyNotificationService : INotificationService
{
    private readonly NotificationQueue _queue;
    private readonly IOptionsMonitor<NotificationOptions> _options;
    private readonly Lock _throttleLock = new();
    private readonly Dictionary<string, DateTime> _lastSentAt = new();

    public NtfyNotificationService(NotificationQueue queue, IOptionsMonitor<NotificationOptions> options)
    {
        _queue = queue;
        _options = options;
    }

    public void NotifyWatched(AniListUser user, int anilistId, int episode, string? mediaTitle, MediaType mediaType)
    {
        var target = user.Notifications;
        if (target is not { IsConfigured: true })
            return;

        var media = string.IsNullOrWhiteSpace(mediaTitle) ? $"AniList #{anilistId.ToStringInvariantCulture()}" : mediaTitle;
        var isMovie = mediaType == MediaType.Movie;
        _queue.Enqueue(
            new QueuedNotification(
                target,
                Title: media,
                Message: isMovie
                    ? $"Watched {media}"
                    : $"Watched episode {episode.ToStringInvariantCulture()} of {media}",
                Tags: isMovie ? "clapper_board" : "tv",
                Priority: "default",
                Click: $"https://anilist.co/anime/{anilistId.ToStringInvariantCulture()}"
            )
        );
    }

    public void NotifyGlobalError(string dedupeKey, string message)
    {
        var target = _options.CurrentValue.Errors;
        if (target is not { IsConfigured: true } || !ShouldSend(dedupeKey))
            return;

        EnqueueError(target, message);
    }

    public void NotifyUserError(AniListUser user, string dedupeKey, string message)
    {
        var target = user.Notifications;
        if (target is not { IsConfigured: true })
            return;

        // The global sink already covers this topic when it is also the errors topic.
        if (
            _options.CurrentValue.Errors is { IsConfigured: true } global
            && string.Equals(global.TopicUrl, target.TopicUrl, StringComparison.Ordinal)
        )
            return;

        if (!ShouldSend(dedupeKey))
            return;

        EnqueueError(target, message);
    }

    private void EnqueueError(NotificationTarget target, string message) =>
        _queue.Enqueue(
            new QueuedNotification(
                target,
                Title: "PlexAniListSync error",
                Message: message,
                Tags: "warning",
                Priority: "high",
                Click: null
            )
        );

    private bool ShouldSend(string dedupeKey)
    {
        var now = DateTime.UtcNow;
        var throttle = _options.CurrentValue.ErrorThrottle;
        lock (_throttleLock)
        {
            if (_lastSentAt.TryGetValue(dedupeKey, out var last) && now - last < throttle)
                return false;

            _lastSentAt[dedupeKey] = now;
            return true;
        }
    }
}
