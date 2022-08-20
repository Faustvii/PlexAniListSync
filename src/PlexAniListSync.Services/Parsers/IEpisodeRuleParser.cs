using PlexAniListSync.Models.Mappings;

namespace PlexAniListSync.Services.Parsers
{
    public interface IEpisodeRuleParser
    {
        IReadOnlyList<EpisodeRuleMapping> ParseRules(string ruleContent);
    }
}
