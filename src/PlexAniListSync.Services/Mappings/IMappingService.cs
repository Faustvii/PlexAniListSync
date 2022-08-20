namespace PlexAniListSync.Services.Mappings
{
    public interface IMappingService
    {
        int GetAniListIdFromTitle(string title, int season);
        int GetEpisodeNumber(int episode, int anilistId);
    }
}
