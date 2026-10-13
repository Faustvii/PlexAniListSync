using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlexAniListSync.Models.Notifications;
using PlexAniListSync.Services.Notifications;
using Xunit;

namespace PlexAniListSync.UnitTests.Notifications;

public class NotificationWorkerTests
{
    private sealed class CapturingHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public List<string?> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status);
        }
    }

    private static NotificationWorker Build(CapturingHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(NotificationWorker.HttpClientName)).Returns(() => new HttpClient(handler));
        return new NotificationWorker(new NotificationQueue(), factory.Object, NullLogger<NotificationWorker>.Instance);
    }

    private static QueuedNotification Notification(NotificationTarget? target = null) =>
        new(
            target ?? new NotificationTarget { TopicUrl = "https://ntfy.local/my-sync", AccessToken = "tk_secret" },
            Title: "Cowboy Bebop",
            Message: "Watched episode 7 of Cowboy Bebop",
            Tags: "tv",
            Priority: "default",
            Click: "https://anilist.co/anime/1"
        );

    [Fact]
    public async Task Publish_SendsNtfyRequestWithHeadersAndBearerToken()
    {
        var handler = new CapturingHandler();
        var worker = Build(handler);

        await worker.PublishAsync(Notification(), CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://ntfy.local/my-sync", request.RequestUri!.ToString());
        Assert.Equal("Watched episode 7 of Cowboy Bebop", handler.Bodies[0]);
        Assert.Equal("Bearer tk_secret", request.Headers.Authorization!.ToString());
        Assert.Equal("Cowboy Bebop", Assert.Single(request.Headers.GetValues("Title")));
        Assert.Equal("tv", Assert.Single(request.Headers.GetValues("Tags")));
        Assert.Equal("https://anilist.co/anime/1", Assert.Single(request.Headers.GetValues("Click")));
        // default priority is ntfy's default - header omitted
        Assert.False(request.Headers.Contains("Priority"));
    }

    [Fact]
    public async Task Publish_NonDefaultPriority_SendsPriorityHeader()
    {
        var handler = new CapturingHandler();
        var worker = Build(handler);

        var notification = Notification() with { Priority = "high" };
        await worker.PublishAsync(notification, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("high", Assert.Single(request.Headers.GetValues("Priority")));
    }

    [Fact]
    public async Task Publish_WithoutAccessToken_OmitsAuthorizationHeader()
    {
        var handler = new CapturingHandler();
        var worker = Build(handler);

        var notification = Notification(new NotificationTarget { TopicUrl = "https://ntfy.local/open-sync" });
        await worker.PublishAsync(notification, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task Publish_ServerError_DoesNotThrow()
    {
        var handler = new CapturingHandler(HttpStatusCode.InternalServerError);
        var worker = Build(handler);

        // a dead ntfy server must never bubble up into the sync path
        await worker.PublishAsync(Notification(), CancellationToken.None);

        Assert.Single(handler.Requests);
    }
}
