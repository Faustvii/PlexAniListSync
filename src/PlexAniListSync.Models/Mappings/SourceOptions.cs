namespace PlexAniListSync.Models.Mappings;

public class SourceOptions
{
    public const string Key = "Sources";
    public int CheckForUpdateEveryHours { get; set; } = 6;
    public string[] AnilistMappingUrls { get; set; } = Array.Empty<string>();
    public string[] EpisodeRuleUrls { get; set; } = Array.Empty<string>();
}
