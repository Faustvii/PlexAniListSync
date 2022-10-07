using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Services.AniList;
using Xunit;

namespace PlexAniListSync.IntegrationTests.AniList;

public class AniListTests
{
    [Theory]
    [InlineData(108241, "Gleipnir", 1)]
    [InlineData(139587, "Tensei Shitara Ken Deshita", 1)]
    public async Task CanRetrieveAniListIdByTitleOnAPI(int expectedId, string title, int season)
    {
        var anilistOptions = new AniListOptions
        {
            TestMode = true,
            Users = new[]
            {
                new AniListOptions.AniListUser { PlexUsernames = new[] { "" }, Token = "" }
            }
        };
        var options = Options.Create(anilistOptions);
        var logger = Mock.Of<ILogger<AniListService>>();
        var service = new AniListService(options, logger, new AniListNet.AniClient());

        var anilistId = await service.FindShowAsync(title, season);

        Assert.Equal(expectedId, anilistId);
    }
}
