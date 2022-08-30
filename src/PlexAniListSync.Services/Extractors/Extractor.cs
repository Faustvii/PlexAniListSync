using System.Text.RegularExpressions;

namespace PlexAniListSync.Services.Extractors;

public class Extractor : IExtractor
{
    private readonly Regex _aniDBRegex = new(@"(?!com\.plexapp\.agents\.hama:\/\/)(?<=anidb-|anidb2-|anidb3-|anidb4-)([0-9]+)", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));
    private readonly Regex _tvDBRegex = new(@"(?!com\.plexapp\.agents\.hama:\/\/)(?<=tvdb-|tvdb2-|tvdb3-|tvdb4-|tvdb5-)([0-9]+)", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    public string? ExtractShowId(string plexGuid)
    {
        if (plexGuid.Contains("anidb", StringComparison.OrdinalIgnoreCase))
        {
            return _aniDBRegex.Match(plexGuid).Value;
        }

        if (plexGuid.Contains("tvdb", StringComparison.OrdinalIgnoreCase))
        {
            return _tvDBRegex.Match(plexGuid).Value;
        }

        return null;
    }

    public Task<int> GetAnilistIdFromTVDBAsync(int TVDBId)
    {
        throw new NotSupportedException();
    }
}
