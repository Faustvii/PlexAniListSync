using Microsoft.Extensions.Caching.Memory;
using PlexAniListSync.Models.Mappings;

namespace PlexAniListSync.Services.Caching;

public class DataCache : IDataCache
{
    private readonly IMemoryCache _cache;

    public DataCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public IReadOnlyList<AnilistMapping> GetAnilistMapping()
    {
        return _cache.Get<IReadOnlyList<AnilistMapping>>(Constants.AnimeMappingCacheKey);
    }

    public IReadOnlyList<EpisodeRuleMapping> GetEpisodeRuleMappings()
    {
        return _cache.Get<IReadOnlyList<EpisodeRuleMapping>>(Constants.AnimeEpisodeMappingRulesCacheKey);
    }

    public void SetAnilistMappings(IEnumerable<AnilistMapping> mappings)
    {
        _cache.Set(Constants.AnimeMappingCacheKey, mappings.ToList());
    }

    public void SetEpisodeRuleMappings(IEnumerable<EpisodeRuleMapping> rules)
    {
        _cache.Set(Constants.AnimeEpisodeMappingRulesCacheKey, rules.ToList());
    }
}
