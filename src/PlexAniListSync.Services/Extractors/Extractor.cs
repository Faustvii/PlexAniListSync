using System.Text.RegularExpressions;

namespace PlexAniListSync.Services.Extractors;

public class Extractor : IExtractor
{
    private readonly Regex _aniDBRegex = new(@"(?!com\.plexapp\.agents\.hama:\/\/)(?<=anidb-)([0-9]+)");
    private readonly Regex _tvDBRegex = new(@"(?!com\.plexapp\.agents\.hama:\/\/)(?<=tvdb-)([0-9]+)");

    public string? ExtractShowId(string plexGuid)
    {
        if (plexGuid.Contains("anidb", StringComparison.OrdinalIgnoreCase))
            return _aniDBRegex.Match(plexGuid).Value;
        else if (plexGuid.Contains("tvdb", StringComparison.OrdinalIgnoreCase))
            return _tvDBRegex.Match(plexGuid).Value;

        return null;
    }

    public Task<int> GetAnilistIdFromTVDBAsync(int TVDBId)
    {
        throw new NotImplementedException();
    }
}
