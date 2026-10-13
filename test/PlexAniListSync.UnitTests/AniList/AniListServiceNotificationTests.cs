using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlexAniListSync.AniListNet.Objects;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Cache;
using PlexAniListSync.Models.Notifications;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.Notifications;
using Xunit;
using ZiggyCreatures.Caching.Fusion;
using MediaEntryMutation = PlexAniListSync.AniListNet.Parameters.MediaEntryMutation;
using MediaType = PlexAniListSync.Models.Webhook.MediaType;

namespace PlexAniListSync.UnitTests.AniList;

public class AniListServiceNotificationTests
{
    private const string PlexUser = "plexuser";
    private const string Token = "anilist-token";

    // AniListNet objects have private setters (built by the GraphQL parser), so tests
    // fabricate them the same way the library does.
    private static MediaEntry BuildEntry(int progress, int? episodes, string title)
    {
        var entry = Activator.CreateInstance(typeof(MediaEntry), nonPublic: true)!;
        var media = Activator.CreateInstance(typeof(Media), nonPublic: true)!;
        var mediaTitle = Activator.CreateInstance(typeof(MediaTitle), nonPublic: true)!;

        typeof(MediaTitle).GetProperty(nameof(MediaTitle.PreferredTitle))!.SetValue(mediaTitle, title);
        typeof(Media).GetProperty(nameof(Media.Title))!.SetValue(media, mediaTitle);
        typeof(Media).GetProperty(nameof(Media.Episodes))!.SetValue(media, episodes);
        typeof(MediaEntry).GetProperty(nameof(MediaEntry.Media))!.SetValue(entry, media);
        typeof(MediaEntry).GetProperty(nameof(MediaEntry.Progress))!.SetValue(entry, progress);
        typeof(MediaEntry)
            .GetProperty(nameof(MediaEntry.StartDate))!
            .SetValue(entry, Activator.CreateInstance(typeof(Date), nonPublic: true)!);
        return (MediaEntry)entry;
    }

    private static (AniListService Service, Mock<IAniClient> Client, Mock<INotificationService> Notifications) Build(
        AniListOptions.AniListUser user,
        MediaEntry? cachedEntry,
        MediaEntry? savedEntry = null
    )
    {
        var client = new Mock<IAniClient>(MockBehavior.Strict);
        client.Setup(x => x.TryAuthenticateAsync(Token)).ReturnsAsync(true);
        client.Setup(x => x.GetMediaEntryAsync(It.IsAny<int>())).ReturnsAsync(cachedEntry);
        if (savedEntry is not null)
            client
                .Setup(x => x.SaveMediaEntryAsync(It.IsAny<int>(), It.IsAny<MediaEntryMutation>()))
                .ReturnsAsync(savedEntry);

        var notifications = new Mock<INotificationService>();
        var cache = new FusionCache(Options.Create(new FusionCacheOptions()));
        var service = new AniListService(
            Options.Create(new AniListOptions { TestMode = false, Users = new[] { user } }),
            Mock.Of<ILogger<AniListService>>(),
            client.Object,
            cache,
            Options.Create(new CacheOptions()),
            notifications.Object
        );
        return (service, client, notifications);
    }

    [Fact]
    public async Task SuccessfulUpdate_NotifiesWatchedEpisodeWithMediaTitle()
    {
        var user = new AniListOptions.AniListUser
        {
            Token = Token,
            PlexUsernames = new[] { PlexUser },
            Notifications = new NotificationTarget { TopicUrl = "https://ntfy.local/me" },
        };

        var (service, _, notifications) = Build(user, cachedEntry: null, savedEntry: BuildEntry(7, 24, "Cowboy Bebop"));

        await service.UpdateMediaAsync(PlexUser, anilistId: 1, episode: 7, MediaType.Episode);

        notifications.Verify(x => x.NotifyWatched(user, 1, 7, "Cowboy Bebop", MediaType.Episode), Times.Once);
    }

    [Fact]
    public async Task SuccessfulMovieUpdate_PropagatesMovieType()
    {
        var user = new AniListOptions.AniListUser
        {
            Token = Token,
            PlexUsernames = new[] { PlexUser },
            Notifications = new NotificationTarget { TopicUrl = "https://ntfy.local/me" },
        };
        var (service, _, notifications) = Build(user, cachedEntry: null, savedEntry: BuildEntry(1, 1, "Your Name."));

        await service.UpdateMediaAsync(PlexUser, anilistId: 1, episode: 1, MediaType.Movie);

        notifications.Verify(x => x.NotifyWatched(user, 1, 1, "Your Name.", MediaType.Movie), Times.Once);
    }

    [Fact]
    public async Task Rewatch_SkipsUpdateAndDoesNotNotify()
    {
        var user = new AniListOptions.AniListUser
        {
            Token = Token,
            PlexUsernames = new[] { PlexUser },
            Notifications = new NotificationTarget { TopicUrl = "https://ntfy.local/me" },
        };

        var (service, client, notifications) = Build(user, cachedEntry: BuildEntry(7, 24, "Cowboy Bebop"));

        await service.UpdateMediaAsync(PlexUser, anilistId: 1, episode: 7, MediaType.Episode);

        notifications.Verify(
            x =>
                x.NotifyWatched(
                    It.IsAny<AniListOptions.AniListUser>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<MediaType>()
                ),
            Times.Never
        );
        client.Verify(x => x.SaveMediaEntryAsync(It.IsAny<int>(), It.IsAny<MediaEntryMutation>()), Times.Never);
    }

    [Fact]
    public async Task TestMode_SavesNothingAndDoesNotNotify()
    {
        var user = new AniListOptions.AniListUser
        {
            Token = Token,
            PlexUsernames = new[] { PlexUser },
            Notifications = new NotificationTarget { TopicUrl = "https://ntfy.local/me" },
        };
        var client = new Mock<IAniClient>(MockBehavior.Strict);
        client.Setup(x => x.TryAuthenticateAsync(Token)).ReturnsAsync(true);
        client.Setup(x => x.GetMediaEntryAsync(It.IsAny<int>())).ReturnsAsync((MediaEntry?)null);
        var notifications = new Mock<INotificationService>();
        var service = new AniListService(
            Options.Create(new AniListOptions { TestMode = true, Users = new[] { user } }),
            Mock.Of<ILogger<AniListService>>(),
            client.Object,
            new FusionCache(Options.Create(new FusionCacheOptions())),
            Options.Create(new CacheOptions()),
            notifications.Object
        );

        await service.UpdateMediaAsync(PlexUser, anilistId: 1, episode: 7, MediaType.Episode);

        notifications.Verify(
            x =>
                x.NotifyWatched(
                    It.IsAny<AniListOptions.AniListUser>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<MediaType>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task AuthenticationFailure_NotifiesUserError()
    {
        var user = new AniListOptions.AniListUser
        {
            Token = Token,
            PlexUsernames = new[] { PlexUser },
            Notifications = new NotificationTarget { TopicUrl = "https://ntfy.local/me" },
        };
        var client = new Mock<IAniClient>(MockBehavior.Strict);
        client.Setup(x => x.TryAuthenticateAsync(Token)).ReturnsAsync(false);
        var notifications = new Mock<INotificationService>();
        var service = new AniListService(
            Options.Create(new AniListOptions { TestMode = false, Users = new[] { user } }),
            Mock.Of<ILogger<AniListService>>(),
            client.Object,
            new FusionCache(Options.Create(new FusionCacheOptions())),
            Options.Create(new CacheOptions()),
            notifications.Object
        );

        await service.UpdateMediaAsync(PlexUser, anilistId: 1, episode: 7, MediaType.Episode);

        notifications.Verify(
            x =>
                x.NotifyUserError(
                    user,
                    It.IsAny<string>(),
                    It.Is<string>(m => m.Contains("token expired", StringComparison.OrdinalIgnoreCase))
                ),
            Times.Once
        );
        notifications.Verify(
            x =>
                x.NotifyWatched(
                    It.IsAny<AniListOptions.AniListUser>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<MediaType>()
                ),
            Times.Never
        );
    }
}
