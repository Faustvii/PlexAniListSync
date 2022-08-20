namespace PlexAniListSync.Models.Mappings;

public record AnilistSeason
{
    public int Number { get; set; }

    public int AnilistId { get; set; }

    public int? Start { get; set; }
}
