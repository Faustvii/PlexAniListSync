namespace PlexAniListSync.Services.Mappings;

public interface IMappingService
{
    int GetAniListIdFromTitle(string title, int season, int episode);
    int GetEpisodeNumber(int episode, int anilistId);
}
