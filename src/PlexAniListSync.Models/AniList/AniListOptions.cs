namespace PlexAniListSync.Models.AniList;

public class AniListOptions
{
    public const string Key = "Anilist";
    public string Token { get; set; } = string.Empty;
    public bool TestMode { get; set; }
}
