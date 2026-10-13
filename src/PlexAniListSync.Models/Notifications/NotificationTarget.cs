namespace PlexAniListSync.Models.Notifications;

public class NotificationTarget
{
    public string TopicUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(TopicUrl);
}
