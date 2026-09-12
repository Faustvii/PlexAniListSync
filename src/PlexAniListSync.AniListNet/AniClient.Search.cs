using PlexAniListSync.AniListNet.Helpers;
using PlexAniListSync.AniListNet.Objects;
using PlexAniListSync.AniListNet.Parameters;

namespace PlexAniListSync.AniListNet;

public partial class AniClient
{
    public async Task<AniPagination<Media>> SearchMediaAsync(SearchMediaFilter filter,
        AniPaginationOptions? options = null)
    {
        options ??= new AniPaginationOptions();
        var selections = new GqlSelection("Page")
        {
            Parameters = options.ToParameters(),
            Selections = new GqlSelection[]
            {
                new("pageInfo", GqlParser.ParseToSelections<PageInfo>()),
                new("media", GqlParser.ParseToSelections<Media>(), filter.ToParameters())
            }
        };
        var response = await PostRequestAsync(selections);
        var page = response["Page"]!;
        return new AniPagination<Media>(
            GqlParser.ParseFromJson<PageInfo>(page["pageInfo"])!,
            GqlParser.ParseFromJson<Media[]>(page["media"])!
        );
    }
}
