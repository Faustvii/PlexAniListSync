namespace PlexAniListSync.Services.Mappings;

public interface IMappingService
{
    int GetAniListIdFromPlexGuid(string plexGuid, int season, int episode);
    int GetEpisodeNumber(int episode, int anilistId);
}
