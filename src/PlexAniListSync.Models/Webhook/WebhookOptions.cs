namespace PlexAniListSync.Models.Webhook;

public class WebhookOptions
{
    public const string Key = "Webhook";
    public IDictionary<string, string> Users { get; set; } = new Dictionary<string, string>();
}
