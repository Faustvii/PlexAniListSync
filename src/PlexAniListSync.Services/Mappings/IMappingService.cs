namespace PlexAniListSync.Services.Mappings;

public interface IMappingService
{
    int GetAniListIdFromPlexGuid(string plexGuid, int season, int episode);
    (int episodeNumber, int anilistId) GetEpisodeNumber(int episode, int anilistId);
}
