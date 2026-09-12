using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Cache;
using PlexAniListSync.Services.AniList;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace PlexAniListSync.IntegrationTests.AniList;

public class AniListTests
{
    [Theory]
    [InlineData(108241, "Gleipnir", 1)]
    [InlineData(139587, "Tensei Shitara Ken Deshita", 1)]
    [InlineData(171018, "Dan Da Dan", 1)]
    [InlineData(135865, "Saga of Tanya the Evil", 2)]
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
        var client = new AniClientWrapper(new PlexAniListSync.AniListNet.AniClient(), Mock.Of<ILogger<AniClientWrapper>>());
        var cache = new FusionCache(Options.Create(new FusionCacheOptions()));
        var cacheOptions = Options.Create(new CacheOptions());
        var service = new AniListService(options, logger, client, cache, cacheOptions);

        var anilistId = await service.FindShowAsync(title, season);

        Assert.Equal(expectedId, anilistId);
    }

    [Theory]
    [InlineData(2154, "Tekkonkinkreet")]
    [InlineData(133845, "OVERLORD: The Sacred Kingdom")]
    [InlineData(21519, "Your Name.")]
    public async Task CanRetrieveMovieAniListIdByTitleOnAPI(int expectedId, string title)
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
        var client = new AniClientWrapper(new PlexAniListSync.AniListNet.AniClient(), Mock.Of<ILogger<AniClientWrapper>>());
        var cache = new FusionCache(Options.Create(new FusionCacheOptions()));
        var cacheOptions = Options.Create(new CacheOptions());
        var service = new AniListService(options, logger, client, cache, cacheOptions);

        var anilistId = await service.FindMovieAsync(title);

        Assert.Equal(expectedId, anilistId);
    }
}
