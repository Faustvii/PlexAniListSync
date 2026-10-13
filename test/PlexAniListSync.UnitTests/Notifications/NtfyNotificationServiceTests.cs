using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Notifications;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.Notifications;
using Xunit;

namespace PlexAniListSync.UnitTests.Notifications;

public class NtfyNotificationServiceTests
{
    private static NotificationTarget Target(string url = "https://ntfy.local/faust-sync") =>
        new() { TopicUrl = url, AccessToken = "tk_test" };

    private static AniListOptions.AniListUser User(NotificationTarget? notifications = null) =>
        new()
        {
            Token = "token",
            PlexUsernames = new[] { "plexuser" },
            Notifications = notifications,
        };

    private static (NtfyNotificationService Service, NotificationQueue Queue) Build(NotificationOptions? options = null)
    {
        var queue = new NotificationQueue();
        var service = new NtfyNotificationService(
            queue,
            new StaticOptionsMonitor(options ?? new NotificationOptions())
        );
        return (service, queue);
    }

    private static async Task<List<QueuedNotification>> DrainAsync(NotificationQueue queue)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var items = new List<QueuedNotification>();
        try
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
                items.Add(item);
        }
        catch (OperationCanceledException) { }
        return items;
    }

    private sealed class StaticOptionsMonitor(NotificationOptions options) : IOptionsMonitor<NotificationOptions>
    {
        public NotificationOptions CurrentValue => options;

        public NotificationOptions Get(string? name) => options;

        public IDisposable? OnChange(Action<NotificationOptions, string?> listener) => null;
    }

    [Fact]
    public async Task NotifyWatched_WithTopic_EnqueuesWatchedEpisodeMessage()
    {
        var (service, queue) = Build();

        service.NotifyWatched(User(Target()), anilistId: 1, episode: 7, mediaTitle: "Cowboy Bebop", MediaType.Episode);

        var items = await DrainAsync(queue);
        var item = Assert.Single(items);
        Assert.Equal("https://ntfy.local/faust-sync", item.Target.TopicUrl);
        Assert.Equal("tk_test", item.Target.AccessToken);
        Assert.Equal("Cowboy Bebop", item.Title);
        Assert.Equal("Watched episode 7 of Cowboy Bebop", item.Message);
        Assert.Equal("tv", item.Tags);
        Assert.Equal("default", item.Priority);
        Assert.Equal("https://anilist.co/anime/1", item.Click);
    }

    [Fact]
    public async Task NotifyWatched_Movie_MessageOmitsEpisode()
    {
        var (service, queue) = Build();

        service.NotifyWatched(User(Target()), anilistId: 1, episode: 1, mediaTitle: "Your Name.", MediaType.Movie);

        var item = Assert.Single(await DrainAsync(queue));
        Assert.Equal("Watched Your Name.", item.Message);
        Assert.Equal("clapper_board", item.Tags);
    }

    [Fact]
    public async Task NotifyWatched_WithoutTopic_EnqueuesNothing()
    {
        var (service, queue) = Build();

        service.NotifyWatched(
            User(notifications: null),
            anilistId: 1,
            episode: 7,
            mediaTitle: "Cowboy Bebop",
            MediaType.Episode
        );

        Assert.Empty(await DrainAsync(queue));
    }

    [Fact]
    public async Task NotifyWatched_WithoutTitle_FallsBackToAnilistId()
    {
        var (service, queue) = Build();

        service.NotifyWatched(User(Target()), anilistId: 21, episode: 3, mediaTitle: null, MediaType.Episode);

        var item = Assert.Single(await DrainAsync(queue));
        Assert.Equal("Watched episode 3 of AniList #21", item.Message);
    }

    [Fact]
    public async Task NotifyGlobalError_WithErrorsTarget_EnqueuesHighPriorityWarning()
    {
        var options = new NotificationOptions { Enabled = true, Errors = Target("https://ntfy.local/errors") };
        var (service, queue) = Build(options);

        service.NotifyGlobalError("key", "mapping download failed");

        var item = Assert.Single(await DrainAsync(queue));
        Assert.Equal("https://ntfy.local/errors", item.Target.TopicUrl);
        Assert.Equal("PlexAniListSync error", item.Title);
        Assert.Equal("mapping download failed", item.Message);
        Assert.Equal("high", item.Priority);
    }

    [Fact]
    public async Task NotifyGlobalError_WithoutErrorsTarget_EnqueuesNothing()
    {
        var (service, queue) = Build();

        service.NotifyGlobalError("key", "boom");

        Assert.Empty(await DrainAsync(queue));
    }

    [Fact]
    public async Task NotifyGlobalError_SameKeyWithinThrottleWindow_IsSuppressed()
    {
        var options = new NotificationOptions
        {
            Enabled = true,
            Errors = Target(),
            ErrorThrottle = TimeSpan.FromMinutes(10),
        };
        var (service, queue) = Build(options);

        service.NotifyGlobalError("same", "first");
        service.NotifyGlobalError("same", "suppressed");
        service.NotifyGlobalError("other", "different problem still sends");

        var items = await DrainAsync(queue);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Message == "first");
        Assert.Contains(items, x => x.Message == "different problem still sends");
    }

    [Fact]
    public async Task NotifyUserError_WithOwnTopic_EnqueuesToUser()
    {
        var options = new NotificationOptions { Enabled = true, Errors = Target("https://ntfy.local/errors") };
        var (service, queue) = Build(options);

        service.NotifyUserError(User(Target("https://ntfy.local/faust-sync")), "auth:abc", "token expired?");

        var item = Assert.Single(await DrainAsync(queue));
        Assert.Equal("https://ntfy.local/faust-sync", item.Target.TopicUrl);
    }

    [Fact]
    public async Task NotifyUserError_WhenUserTopicIsAlsoGlobalErrorsTopic_IsSkipped()
    {
        var options = new NotificationOptions { Enabled = true, Errors = Target("https://ntfy.local/same") };
        var (service, queue) = Build(options);

        service.NotifyUserError(User(Target("https://ntfy.local/same")), "auth:abc", "token expired?");

        Assert.Empty(await DrainAsync(queue));
    }

    [Fact]
    public async Task NotifyUserError_WithoutUserTopic_EnqueuesNothing()
    {
        var (service, queue) = Build();

        service.NotifyUserError(User(notifications: null), "auth:abc", "token expired?");

        Assert.Empty(await DrainAsync(queue));
    }
}
