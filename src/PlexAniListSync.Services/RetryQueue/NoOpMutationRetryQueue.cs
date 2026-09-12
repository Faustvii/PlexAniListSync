using Microsoft.Extensions.Logging;
using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.RetryQueue;

public sealed class NoOpMutationRetryQueue : IMutationRetryQueue
{
    private readonly ILogger<NoOpMutationRetryQueue> _logger;

    public NoOpMutationRetryQueue(ILogger<NoOpMutationRetryQueue> logger) => _logger = logger;

    public Task<bool> EnqueueAsync(WebhookData data, DateTimeOffset now, CancellationToken cancellationToken)
    {
        _logger.LogRetryQueueDisabledDroppingMutation(data.ShowTitle, data.Episode);
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<QueuedMutation>> DequeueDueAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<QueuedMutation>>(Array.Empty<QueuedMutation>());

    public Task RemoveAsync(QueuedMutation entry, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RescheduleAsync(QueuedMutation entry, DateTimeOffset nextAttemptAt, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
