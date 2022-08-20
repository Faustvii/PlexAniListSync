namespace PlexAniListSync.Models.Webhook;

public record WebhookData
{
    public string ShowTitle { get; init; } = string.Empty;
    public int Season { get; init; }
    public int Episode { get; init; }
    public string PlexGuid { get; init; } = string.Empty;
    public string EpisodeRatingKey { get; init; } = string.Empty;
    public string SeasonRatingKey { get; init; } = string.Empty;
    public string ShowRatingKey { get; init; } = string.Empty;
    public MediaType Type { get; init; }
    public string User { get; init; } = string.Empty;
    public bool IsAniDb => PlexGuid.Contains("anidb", StringComparison.OrdinalIgnoreCase);
}
