using YamlDotNet.Serialization;

namespace PlexAniListSync.Services.Parsers;

public class AnilistTVParser : IAnilistTVParser
{
    public IReadOnlyList<Models.Mappings.AnilistMapping> ParseMappings(
        string anilistTVMappingContent
    )
    {
        var deserializer = new Deserializer();
        var parsed = deserializer.Deserialize<AniListMappings>(anilistTVMappingContent);
        return parsed.Entries
            .Select(
                x =>
                    new Models.Mappings.AnilistMapping
                    {
                        Title = x.Title,
                        Synonyms = x.Synonyms,
                        Seasons = x.Seasons
                            .Select(
                                s =>
                                    new Models.Mappings.AnilistSeason
                                    {
                                        AnilistId = s.AnilistId,
                                        Number = s.Number,
                                        Start = s.Start
                                    }
                            )
                            .ToArray()
                    }
            )
            .ToList();
    }

    private record AniListMappings
    {
        [YamlMember(Alias = "entries", ApplyNamingConventions = false)]
        public IEnumerable<AniListMappingModel> Entries { get; set; } =
            Array.Empty<AniListMappingModel>();
    }

    private record AniListMappingModel
    {
        [YamlMember(Alias = "title", ApplyNamingConventions = false)]
        public string Title { get; set; } = string.Empty;

        [YamlMember(Alias = "synonyms", ApplyNamingConventions = false)]
        public string[] Synonyms { get; set; } = Array.Empty<string>();

        [YamlMember(Alias = "seasons", ApplyNamingConventions = false)]
        public Season[] Seasons { get; set; } = Array.Empty<Season>();
    }

    private record Season
    {
        [YamlMember(Alias = "season", ApplyNamingConventions = false)]
        public int Number { get; set; }

        [YamlMember(Alias = "anilist-id", ApplyNamingConventions = false)]
        public int AnilistId { get; set; }

        [YamlMember(Alias = "start", ApplyNamingConventions = false)]
        public int? Start { get; set; }
    }
}
