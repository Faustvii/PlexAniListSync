namespace PlexAniListSync.Models.Plex;

public class PlexOptions
{
    public const string Key = "Plex";
    public string Host { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
