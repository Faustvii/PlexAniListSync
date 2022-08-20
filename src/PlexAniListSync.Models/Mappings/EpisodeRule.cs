namespace PlexAniListSync.Models.Mappings;

public record EpisodeRule
{
    public string MyAnimeListId { get; init; }
    public string KitsuId { get; init; }
    public string AnilistId { get; init; }
    public int[] EpisodeRange { get; init; }

    public EpisodeRule(string myAnimeListId, string kitsuId, string anilistId, int[] episodeRange)
    {
        MyAnimeListId = myAnimeListId;
        KitsuId = kitsuId;
        AnilistId = anilistId;
        EpisodeRange = episodeRange;
    }
}
