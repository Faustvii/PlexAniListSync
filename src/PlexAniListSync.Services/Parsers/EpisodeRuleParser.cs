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
                throw new ArgumentException($"Could not parse episode range from '{rangeSection}'", nameof(ruleSection));
            }

            var (startRange, _) = rangeSection[0].ToIntInvariantCulture();
            var (endRange, endRangeParsed) = rangeSection[1].ToIntInvariantCulture();

            episodeRange = Enumerable.Range(startRange, endRangeParsed ? endRange - startRange + 1 : 1)
                .ToArray();

        }
        else
        {
            var (episode, _) = episodeRangeSection.ToIntInvariantCulture();
            episodeRange = new[] { episode };
        }

        return new EpisodeRule(myAnimeListId, kitsuId, anilistId, episodeRange);
    }
}
