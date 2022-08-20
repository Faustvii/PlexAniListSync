using PlexAniListSync.Models.Mappings;

namespace PlexAniListSync.Services.Caching
{
    public interface IDataCache
    {
        IReadOnlyList<EpisodeRuleMapping> GetEpisodeRuleMappings();
        void SetEpisodeRuleMappings(IEnumerable<EpisodeRuleMapping> rules);

        IReadOnlyList<AnilistMapping> GetAnilistMapping();
        void SetAnilistMappings(IEnumerable<AnilistMapping> mappings);
    }
}
