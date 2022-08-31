namespace PlexAniListSync.Services.AniList;

public interface IAniListService
{
    Task<int?> FindShowAsync(string title, int season);
    Task UpdateShowAsync(string plexUsername, int anilistId, int episode);
}
