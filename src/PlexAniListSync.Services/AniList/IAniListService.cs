namespace PlexAniListSync.Services.AniList;

public interface IAniListService
{
    Task<int?> FindShow(string title, int season);
    Task UpdateShow(int anilistId, int episode);
}
