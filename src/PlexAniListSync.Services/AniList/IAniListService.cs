using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.AniList;

public interface IAniListService
{
    Task<int?> FindShowAsync(string title, int season);
    Task<int?> FindMovieAsync(string title);
    Task UpdateMediaAsync(string plexUsername, int anilistId, int episode, MediaType mediaType);
}
