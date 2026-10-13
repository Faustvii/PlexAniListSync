using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.Notifications;

public interface INotificationService
{
    void NotifyWatched(
        AniListOptions.AniListUser user,
        int anilistId,
        int episode,
        string? mediaTitle,
        MediaType mediaType
    );

    void NotifyGlobalError(string dedupeKey, string message);

    void NotifyUserError(AniListOptions.AniListUser user, string dedupeKey, string message);
}
