namespace PlexAniListSync.Services.Extractors;

public interface IExtractor
{
    string? ExtractShowId(string plexGuid);
    
    Task<int> GetAnilistIdFromTVDBAsync(int TVDBId);
}
