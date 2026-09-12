using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Cache;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.AniList;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

namespace PlexAniListSync.UnitTests.AniList;

public class AniListServiceCachingTests
{
    private const string PlexUser = "plexuser";
    private const string Token = "super-secret-token";

    private static AniListService BuildService(IAniClient client)
    {
        var anilistOptions = Options.Create(
            new AniListOptions
            {
                TestMode = true, // no real mutation; exercises only the read/cache path
                Users = new[]
                {
                    new AniListOptions.AniListUser { PlexUsernames = new[] { PlexUser }, Token = Token }
                }
            }
        );
        var cache = new FusionCache(Options.Create(new FusionCacheOptions()));
        return new AniListService(
            anilistOptions,
            Mock.Of<ILogger<AniListService>>(),
            client,
            cache,
            Options.Create(new CacheOptions())
        );
    }

    [Fact]
    public async Task RepeatedEpisodes_ReadTheMediaEntryFromAniListOnlyOnce()
    {
        var client = new Mock<IAniClient>(MockBehavior.Strict);
        client.Setup(x => x.TryAuthenticateAsync(Token)).ReturnsAsync(true);
        client.Setup(x => x.GetMediaEntryAsync(It.IsAny<int>())).ReturnsAsync((PlexAniListSync.AniListNet.Objects.MediaEntry?)null);
        var service = BuildService(client.Object);

        await service.UpdateMediaAsync(PlexUser, anilistId: 187538, episode: 7, MediaType.Episode);
        await service.UpdateMediaAsync(PlexUser, anilistId: 187538, episode: 8, MediaType.Episode);

        client.Verify(x => x.GetMediaEntryAsync(187538), Times.Once);
    }

    [Fact]
    public async Task DifferentAnilistIds_AreCachedSeparately()
    {
        var client = new Mock<IAniClient>(MockBehavior.Strict);
        client.Setup(x => x.TryAuthenticateAsync(Token)).ReturnsAsync(true);
        client.Setup(x => x.GetMediaEntryAsync(It.IsAny<int>())).ReturnsAsync((PlexAniListSync.AniListNet.Objects.MediaEntry?)null);
        var service = BuildService(client.Object);

        await service.UpdateMediaAsync(PlexUser, anilistId: 111, episode: 1, MediaType.Episode);
        await service.UpdateMediaAsync(PlexUser, anilistId: 222, episode: 1, MediaType.Episode);

        client.Verify(x => x.GetMediaEntryAsync(111), Times.Once);
        client.Verify(x => x.GetMediaEntryAsync(222), Times.Once);
    }

    [Fact]
    public void MediaEntryCacheKey_DoesNotLeakTheRawToken()
    {
        var key = AniListService.MediaEntryCacheKey(Token, 187538);

        Assert.DoesNotContain(Token, key, System.StringComparison.Ordinal);
        Assert.Contains("187538", key, System.StringComparison.Ordinal);
        // stable + deterministic for the same inputs
        Assert.Equal(key, AniListService.MediaEntryCacheKey(Token, 187538));
        Assert.NotEqual(key, AniListService.MediaEntryCacheKey("other-token", 187538));
    }

    [Theory]
    [InlineData("Black Torch", "black torch")]
    [InlineData("  Black   Torch  ", "black torch")]
    [InlineData("Don't Look Up", "dont look up")]
    public void NormalizeTitle_CollapsesCaseWhitespaceAndAnnoyingCharacters(string input, string expected)
    {
        Assert.Equal(expected, AniListService.NormalizeTitle(input));
    }
}
