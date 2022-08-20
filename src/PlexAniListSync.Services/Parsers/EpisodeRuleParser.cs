using PlexAniListSync.Models.Mappings;

namespace PlexAniListSync.Services.Parsers
{
    public class EpisodeRuleParser : IEpisodeRuleParser
    {
        public IReadOnlyList<EpisodeRuleMapping> ParseRules(string ruleContent)
        {
            var episodeMappings = ruleContent
            .Split('\n', options: StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(x => !x.StartsWith("::rules")) // Skip until we get to the ::rules section
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
                throw new ArgumentException($"We were unable to parse '{rawEpisodeMapping}'");

            var from = ParseRule(mappingSections[0]);
            var to = ParseRule(mappingSections[1]);
            return new EpisodeRuleMapping(from, to);
        }

        private static EpisodeRule ParseRule(string ruleSection)
        {
            //41380|43367|116242:13-24
            //6682|4662|6682:13
            var sections = ruleSection.Split(':');
            if (sections.Length != 2)
                throw new ArgumentException($"Could not parse sections from '{ruleSection}'");
            var ids = sections[0].Split('|');
            if (ids.Length != 3)
                throw new ArgumentException($"Could not parse ids from '{ids}'");
            var myAnimeListId = ids[0];
            var kitsuId = ids[1];
            var anilistId = ids[2];
            int[] episodeRange;
            var episodeRangeSection = sections[1].Trim('!');
            var isRange = episodeRangeSection.Contains('-');
            if (isRange)
            {
                var rangeSection = episodeRangeSection.Split('-');
                if (rangeSection.Length != 2)
                    throw new ArgumentException($"Could not parse episode range from '{rangeSection}'");

                if (!int.TryParse(rangeSection[0], out var _) || !int.TryParse(rangeSection[1], out var _))
                    Console.WriteLine($"TRIED TO PARSE {rangeSection[0]} {rangeSection[1]} to int!");

                _ = int.TryParse(rangeSection[0], out var startRange);
                var endRangeParsed = int.TryParse(rangeSection[1], out var endRange);

                episodeRange = Enumerable.Range(startRange, endRangeParsed ? endRange - startRange + 1 : 1)
                    .ToArray();

            }
            else
            {
                if (!int.TryParse(episodeRangeSection, out var _))
                    Console.WriteLine($"TRIED TO PARSE {episodeRangeSection} to int!");
                episodeRange = new[] { int.Parse(episodeRangeSection) };
            }

            return new EpisodeRule(myAnimeListId, kitsuId, anilistId, episodeRange);
        }
    }
}
