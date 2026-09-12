using System.Net;
using PlexAniListSync.AniListNet.Helpers;
using PlexAniListSync.AniListNet.Objects;

namespace PlexAniListSync.AniListNet;

public partial class AniClient
{
    /// <summary>
    /// Returns the <see cref="MediaEntry"/> for the currently authenticated user, or null if not found.
    /// </summary>
    /// <exception cref="AniException"></exception>
    public async Task<MediaEntry?> GetMediaEntryAsync(int mediaId)
    {
        if (!IsAuthenticated)
            throw new AniException(HttpStatusCode.Unauthorized, "Client is not authenticated");

        var selections = new GqlSelection("Media")
        {
            Parameters = new GqlParameter[] { new("id", mediaId) },
            Selections = new GqlSelection[] { new("mediaListEntry", GqlParser.ParseToSelections<MediaEntry>()) }
        };

        try
        {
            var response = await PostRequestAsync(selections);
            return GqlParser.ParseFromJson<MediaEntry?>(response["Media"]!["mediaListEntry"]);
        }
        catch (AniException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
