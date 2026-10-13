namespace PlexAniListSync.Models.Notifications;

public class NotificationOptions
{
    public const string Key = "Notifications";

    public bool Enabled { get; set; }

    public NotificationTarget? Errors { get; set; }

    public TimeSpan ErrorThrottle { get; set; } = TimeSpan.FromMinutes(10);
}
