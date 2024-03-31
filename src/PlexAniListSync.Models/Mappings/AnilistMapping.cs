namespace PlexAniListSync.Models.Mappings;

public record AnilistMapping
{
    public string Title { get; set; } = string.Empty;

    public string[] LookupIds { get; set; } = Array.Empty<string>();

    public AnilistSeason[] Seasons { get; set; } = Array.Empty<AnilistSeason>();
}
