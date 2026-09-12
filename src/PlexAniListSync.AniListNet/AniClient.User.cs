using PlexAniListSync.AniListNet.Helpers;
using PlexAniListSync.AniListNet.Objects;
using PlexAniListSync.AniListNet.Parameters;

namespace PlexAniListSync.AniListNet;

public partial class AniClient
{
    public async Task<User> GetAuthenticatedUserAsync()
    {
        var selections = new GqlSelection("Viewer")
        {
            Selections = GqlParser.ParseToSelections<User>()
        };
        var response = await PostRequestAsync(selections);
        return GqlParser.ParseFromJson<User>(response["Viewer"])!;
    }

    /// <summary>
    /// Create or update a media entry.
    /// </summary>
    public async Task<MediaEntry> SaveMediaEntryAsync(int mediaId, MediaEntryMutation mutation)
    {
        var selections = new GqlSelection("SaveMediaListEntry")
        {
            Parameters = new GqlParameter[] { new("mediaId", mediaId) }.Concat(mutation.ToParameters()).ToArray(),
            Selections = GqlParser.ParseToSelections<MediaEntry>()
        };
        var response = await PostRequestAsync(selections, true);
        return GqlParser.ParseFromJson<MediaEntry>(response["SaveMediaListEntry"])!;
    }
}
