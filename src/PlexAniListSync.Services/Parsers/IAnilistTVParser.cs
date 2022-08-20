namespace PlexAniListSync.Services.Parsers
{
    public interface IAnilistTVParser
    {
        IReadOnlyList<Models.Mappings.AnilistMapping> ParseMappings(string anilistTVMappingContent);
    }
}
