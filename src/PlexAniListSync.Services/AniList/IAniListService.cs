using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.AniList;

public interface IAniListService
{
    ValueTask<int?> FindShowAsync(string title, int season);
    ValueTask<int?> FindMovieAsync(string title);
    Task UpdateMediaAsync(string plexUsername, int anilistId, int episode, MediaType mediaType);
}
