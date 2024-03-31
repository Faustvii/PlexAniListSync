using YamlDotNet.Serialization;

namespace PlexAniListSync.Services.Parsers;

public class AnilistTVParser : IAnilistTVParser
{
    private readonly IDeserializer _deserializer;

    public AnilistTVParser()
    {
        _deserializer = new DeserializerBuilder().Build();
    }

    public IReadOnlyList<Models.Mappings.AnilistMapping> ParseMappings(string anilistTVMappingContent)
    {
        var parsed = _deserializer.Deserialize<AniListMappings>(anilistTVMappingContent);
        return parsed.Entries
            .Select(
                x =>
                    new Models.Mappings.AnilistMapping
                    {
                        Title = x.Title,
                        LookupIds = new[] { x.PlexGuid, x.Imdb, x.Tmdb, x.Tvdb }
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => x!)
                            .ToArray(),
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
        public IEnumerable<AniListMappingModel> Entries { get; set; } = Array.Empty<AniListMappingModel>();
    }

    private record AniListMappingModel
    {
        [YamlMember(Alias = "title", ApplyNamingConventions = false)]
        public string Title { get; set; } = string.Empty;

        [YamlMember(Alias = "guid", ApplyNamingConventions = false)]
        public string PlexGuid { get; set; } = string.Empty;

        [YamlMember(Alias = "imdb", ApplyNamingConventions = false)]
        public string? Imdb { get; set; }

        [YamlMember(Alias = "tmdb", ApplyNamingConventions = false)]
        public string? Tmdb { get; set; }

        [YamlMember(Alias = "tvdb", ApplyNamingConventions = false)]
        public string? Tvdb { get; set; }

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
