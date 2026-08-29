using PlexAniListSync.Models.Mappings;

namespace PlexAniListSync.Services.Parsers;

public class EpisodeRuleParser : IEpisodeRuleParser
{
    public IReadOnlyList<EpisodeRuleMapping> ParseRules(string ruleContent)
    {
        var episodeMappings = ruleContent
            .Split('\n', options: StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(x => !x.StartsWith("::rules", StringComparison.OrdinalIgnoreCase)) // Skip until we get to the ::rules section
            .Skip(1) // Skip the ::rules line
            .Where(x => !x.StartsWith('#'))
            .Select(x => x.TrimStart('-').Trim())
            .Select(x => ParseEpisodeRuleMapping(x))
            .ToList();

        return episodeMappings;
    }
// # This file includes anime relation data for Taiga. It is used to redirect an
// # episode to another, which is required to handle special episodes and the case
// # where fansub groups use continuous numbering scheme in their releases.
// #
// # Rules are sorted alphabetically by anime title. Rule syntax is:
// #
// #   10001|10002|10003:14-26 -> 20001|20002|20003:1-13!
// #   └─┬─┘ └─┬─┘ └─┬─┘ └─┬─┘    └─┬─┘ └─┬─┘ └─┬─┘ └─┬─┘
// #     1     2     3     4        1     2     3     4
// #
// #   (1) MyAnimeList ID
// #       <https://myanimelist.net/anime/{id}/{title}>
// #   (2) Kitsu ID
// #       <https://kitsu.io/api/edge/anime?filter[text]={title}>
// #   (3) AniList ID
// #       <https://anilist.co/anime/{id}/{title}>
// #   (4) Episode number or range
// #
// #   - "?" is used for unknown values.
// #   - "~" is used to repeat the source ID.
// #   - "!" suffix is shorthand for creating a new rule where destination ID is
// #     redirected to itself.
    private static EpisodeRuleMapping ParseEpisodeRuleMapping(string rawEpisodeMapping)
    {
        var mappingSections = rawEpisodeMapping.Split("->");
        if (mappingSections.Length != 2)
        {
            throw new ArgumentException($"We were unable to parse '{rawEpisodeMapping}'", nameof(rawEpisodeMapping));
        }

        var from = ParseRule(mappingSections[0]);
        var to = ParseRule(mappingSections[1]);
        return new EpisodeRuleMapping(from, to);
    }

    private static EpisodeRule ParseRule(string ruleSection)
    {
        var sections = ruleSection.Split(':');
        if (sections.Length != 2)
        {
            throw new ArgumentException($"Could not parse sections from '{ruleSection}'", nameof(ruleSection));
        }

        var ids = sections[0].Split('|');
        if (ids.Length != 3)
        {
            throw new ArgumentException($"Could not parse ids from '{ids}'", nameof(ruleSection));
        }

        var myAnimeListId = ids[0];
        var kitsuId = ids[1];
        var anilistId = ids[2];
        int[] episodeRange;
        var episodeRangeSection = sections[1].Trim('!');
        var isRange = episodeRangeSection.Contains('-', StringComparison.OrdinalIgnoreCase);
        if (isRange)
        {
            var rangeSection = episodeRangeSection.Split('-');
            if (rangeSection.Length != 2)
            {
                throw new ArgumentException(
                    $"Could not parse episode range from '{rangeSection}'",
                    nameof(ruleSection)
                );
            }

            var (startRange, _) = rangeSection[0].ToIntInvariantCulture();
            var (endRange, endRangeParsed) = rangeSection[1].ToIntInvariantCulture();

            episodeRange = Enumerable.Range(startRange, endRangeParsed ? endRange - startRange + 1 : 1).ToArray();
        }
        else
        {
            var (episode, _) = episodeRangeSection.ToIntInvariantCulture();
            episodeRange = new[] { episode };
        }

        return new EpisodeRule(myAnimeListId, kitsuId, anilistId, episodeRange);
    }
}
