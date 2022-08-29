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
        var anilistId = GetAniListIdFromTitle(title, season, episode);
        var episodeNumber = GetEpisodeNumber(episode, anilistId);
        return (anilistId, episodeNumber);
    }

    public int GetAniListIdFromTitle(string title, int season, int episode)
    {
        var anilistMappings = _cache.GetAnilistMapping();

        var anime = anilistMappings.FirstOrDefault(x => x.Title.Equals(title, StringComparison.OrdinalIgnoreCase) || x.Synonyms.Any(s => s.Equals(title, StringComparison.OrdinalIgnoreCase)));
        if (anime is null)
            return 0;

        var seasons = anime.Seasons
            .Where(x => x.Number == season)
            .ToList();
        if (seasons.Count == 1)
            return seasons.First().AnilistId;

        var animeSeason = seasons.FirstOrDefault(x => x.Start <= episode);
        if (animeSeason is not null)
            return animeSeason.AnilistId;

        return anime.Seasons.FirstOrDefault()?.AnilistId ?? 0;
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
