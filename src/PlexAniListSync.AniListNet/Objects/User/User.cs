using PlexAniListSync.AniListNet.Helpers;

namespace PlexAniListSync.AniListNet.Objects;

public class User
{
    /// <summary>
    /// The ID of the user.
    /// </summary>
    [GqlSelection("id")]
    public int Id { get; private set; }

    /// <summary>
    /// The name of the user.
    /// </summary>
    [GqlSelection("name")]
    public string Name { get; private set; } = null!;
}
