using PlexAniListSync.AniListNet.Helpers;

namespace PlexAniListSync.AniListNet.Objects;

public class MediaTitle
{
    /// <summary>
    /// The romanization of the native language title.
    /// </summary>
    [GqlSelection("romaji")]
    public string RomajiTitle { get; init; } = null!;

    /// <summary>
    /// The official english title.
    /// </summary>
    [GqlSelection("english")]
    public string? EnglishTitle { get; init; }

    /// <summary>
    /// Official title in it's native language.
    /// </summary>
    [GqlSelection("native")]
    public string NativeTitle { get; init; } = null!;

    /// <summary>
    /// The currently authenticated users preferred title language. Default romaji for non-authenticated.
    /// </summary>
    [GqlSelection("userPreferred")]
    public string PreferredTitle { get; init; } = null!;
}
