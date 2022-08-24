using System.Globalization;
using PlexAniListSync.Services.Caching;

namespace PlexAniListSync.Services.Mappings;

public class MappingService : IMappingService
{
    private readonly IDataCache _cache;

    public MappingService(IDataCache cache)
    {
        _cache = cache;
    }

    public (int anilistId, int episodeNumber) MapPlexData(string title, int season, int episode)
    {
        var anilistId = GetAniListIdFromTitle(title, season);
        var episodeNumber = GetEpisodeNumber(episode, anilistId);
        return (anilistId, episodeNumber);
    }

    public int GetAniListIdFromTitle(string title, int season)
    {
        var anilistMappings = _cache.GetAnilistMapping();

        var anime = anilistMappings.FirstOrDefault(x => x.Title.Equals(title, StringComparison.OrdinalIgnoreCase) || x.Synonyms.Any(s => s.Equals(title, StringComparison.OrdinalIgnoreCase)));

        return anime == null ? 0 : anime.Seasons.First(x => x.Number == season).AnilistId;
    }

    public int GetEpisodeNumber(int episode, int anilistId)
    {
        var episodeMappings = _cache.GetEpisodeRuleMappings();

        var episodeMapping = episodeMappings.FirstOrDefault(x => x.To.AnilistId == anilistId.ToString(CultureInfo.InvariantCulture));

        if (episodeMapping == null)
        {
            return episode;
        }

        if (episodeMapping.To.EpisodeRange.Contains(episode))
        {
            return episode;
        }

        var episodeIndex = Array.IndexOf(episodeMapping.From.EpisodeRange, episode);
        var episodeNumber = episodeMapping.To.EpisodeRange[episodeIndex];
        return episodeNumber;
    }
}
