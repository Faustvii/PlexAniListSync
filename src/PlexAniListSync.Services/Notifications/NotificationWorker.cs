using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PlexAniListSync.Services.Notifications;

internal sealed class NotificationWorker : BackgroundService
{
    internal const string HttpClientName = "ntfy";

    private static readonly TimeSpan PublishTimeout = TimeSpan.FromSeconds(3);

    private readonly NotificationQueue _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        NotificationQueue queue,
        IHttpClientFactory httpClientFactory,
        ILogger<NotificationWorker> logger
    )
    {
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var notification in _queue.ReadAllAsync(stoppingToken))
                await PublishAsync(notification, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    internal async Task PublishAsync(QueuedNotification notification, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(PublishTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        using var request = new HttpRequestMessage(HttpMethod.Post, notification.Target.TopicUrl)
        {
            Content = new StringContent(notification.Message, Encoding.UTF8, "text/plain"),
        };
        request.Headers.TryAddWithoutValidation("Title", notification.Title);
        request.Headers.TryAddWithoutValidation("Tags", notification.Tags);
        if (!string.Equals(notification.Priority, "default", StringComparison.Ordinal))
            request.Headers.TryAddWithoutValidation("Priority", notification.Priority);
        if (notification.Click is not null)
            request.Headers.TryAddWithoutValidation("Click", notification.Click);
        if (!string.IsNullOrWhiteSpace(notification.Target.AccessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", notification.Target.AccessToken);

        try
        {
            using var response = await _httpClientFactory.CreateClient(HttpClientName).SendAsync(request, linked.Token);
            if (!response.IsSuccessStatusCode)
                _logger.LogNotificationDeliveryError($"status code {(int)response.StatusCode}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogNotificationDeliveryError("timed out");
        }
        catch (Exception ex)
        {
            _logger.LogNotificationDeliveryError(ex.Message);
        }
    }
}
