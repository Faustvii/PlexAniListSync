using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.Notifications;

internal sealed class NoOpNotificationService : INotificationService
{
    public void NotifyWatched(AniListOptions.AniListUser user, int anilistId, int episode, string? mediaTitle, MediaType mediaType)
    {
    }

    public void NotifyGlobalError(string dedupeKey, string message)
    {
    }

    public void NotifyUserError(AniListOptions.AniListUser user, string dedupeKey, string message)
    {
    }
}
