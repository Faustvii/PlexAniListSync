namespace PlexAniListSync.Models.AniList;

public class AniListOptions
{
    public const string Key = "Anilist";
    public AniListUser[] Users { get; set; } = Array.Empty<AniListUser>();
    public bool TestMode { get; set; }

    public class AniListUser
    {
        public string Token { get; set; } = string.Empty;
        public string[] PlexUsernames { get; set; } = Array.Empty<string>();
    }
}
