using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.AniListNet;
using PlexAniListSync.Models.RetryQueue;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.RetryQueue;
using PlexAniListSync.Services.Webhook;

namespace PlexAniListSync.Services.HostedServices;

public sealed class MutationRetryDrainService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<MutationRetryDrainService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMutationRetryQueue _queue;
    private readonly IRateLimitGate _rateLimitGate;
    private readonly RetryQueueOptions _options;
    private readonly SemaphoreSlim _drainLock = new(1, 1);
    private Timer? _timer;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public MutationRetryDrainService(
        ILogger<MutationRetryDrainService> logger,
        IServiceScopeFactory scopeFactory,
        IMutationRetryQueue queue,
        IRateLimitGate rateLimitGate,
        IOptions<RetryQueueOptions> options
    )
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _queue = queue;
        _rateLimitGate = rateLimitGate;
        _options = options.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogHostedServiceStarting(nameof(MutationRetryDrainService));
        _stoppingCts = new CancellationTokenSource();
        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.DrainIntervalSeconds));
        _timer = new Timer(TimerCallback, state: null, interval, interval);
        return Task.CompletedTask;
    }

    private void TimerCallback(object? state)
    {
        _executingTask = DrainAsync(_stoppingCts!.Token);
    }

    internal async Task DrainAsync(CancellationToken cancellationToken)
    {
        if (!await _drainLock.WaitAsync(0, cancellationToken))
            return;

        try
        {
            var now = DateTimeOffset.UtcNow;
            if (_rateLimitGate.IsPaused(now))
                return;

            var due = await _queue.DequeueDueAsync(now, _options.BatchSize, cancellationToken);
            if (due.Count == 0)
                return;

            using var scope = _scopeFactory.CreateScope();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();
            var aniListService = scope.ServiceProvider.GetRequiredService<IAniListService>();

            var groups = await ResolveAndGroupAsync(due, webhookService, now, cancellationToken);
            if (groups is null)
                return;

            await ApplyGroupsAsync(groups, aniListService, now, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedHostedServiceError(nameof(MutationRetryDrainService), ex);
        }
        finally
        {
            _drainLock.Release();
        }
    }

    private async Task<List<RetryGroup>?> ResolveAndGroupAsync(
        IReadOnlyList<QueuedMutation> due,
        IWebhookService webhookService,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        var groups = new Dictionary<(string User, int AnilistId), RetryGroup>();

        foreach (var entry in due)
        {
            if (RetryScheduling.IsExpired(entry.EnqueuedAt, now, _options))
            {
                await _queue.RemoveAsync(entry, cancellationToken);
                _logger.LogRetryQueueEntryExpired(entry.Data.ShowTitle, entry.Data.Episode, entry.Attempts);
                continue;
            }

            ResolvedMutation? resolved;
            try
            {
                resolved = await webhookService.ResolveAsync(entry.Data);
            }
            catch (AniListRateLimitException ex)
            {
                await PauseAndRescheduleAsync(ex, entry, now, cancellationToken);
                return null;
            }

            if (resolved is null)
            {
                await _queue.RemoveAsync(entry, cancellationToken);
                _logger.LogRetryQueueEntryUnresolvable(entry.Data.ShowTitle, entry.Data.Season);
                continue;
            }

            var key = (entry.Data.User, resolved.AnilistId);
            if (groups.TryGetValue(key, out var group))
            {
                group.Entries.Add(entry);
                if (resolved.AnilistEpisode > group.Episode)
                {
                    group.Episode = resolved.AnilistEpisode;
                    group.Type = resolved.Type;
                }
            }
            else
            {
                groups[key] = new RetryGroup(entry.Data.User, resolved.AnilistId, resolved.AnilistEpisode, resolved.Type)
                {
                    Entries = { entry },
                };
            }
        }

        return groups.Values.ToList();
    }

    private async Task ApplyGroupsAsync(
        List<RetryGroup> groups,
        IAniListService aniListService,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        foreach (var group in groups)
        {
            try
            {
                await aniListService.UpdateMediaAsync(group.User, group.AnilistId, group.Episode, group.Type);
                foreach (var entry in group.Entries)
                    await _queue.RemoveAsync(entry, cancellationToken);
                _logger.LogRetryQueueEntryDrained(group.AnilistId, group.Episode, group.Entries.Count);
            }
            catch (AniListRateLimitException ex)
            {
                _rateLimitGate.Pause(RetryScheduling.PauseUntil(ex, now, _options));
                var pausedUntil = _rateLimitGate.PausedUntil;
                foreach (var entry in group.Entries)
                {
                    var next = RetryScheduling.NextAttempt(entry.Attempts + 1, now, _options, pausedUntil);
                    await _queue.RescheduleAsync(entry, next, cancellationToken);
                }
                _logger.LogDrainRateLimitedOnUpdate(group.AnilistId);
                return;
            }
        }
    }

    private async Task PauseAndRescheduleAsync(
        AniListRateLimitException exception,
        QueuedMutation entry,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        _rateLimitGate.Pause(RetryScheduling.PauseUntil(exception, now, _options));
        var next = RetryScheduling.NextAttempt(entry.Attempts + 1, now, _options, _rateLimitGate.PausedUntil);
        await _queue.RescheduleAsync(entry, next, cancellationToken);
        _logger.LogDrainRateLimitedOnResolve(entry.Data.Episode);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogHostedServiceStopping(nameof(MutationRetryDrainService));
        _timer?.Change(Timeout.Infinite, 0);
        if (_stoppingCts is not null)
            await _stoppingCts.CancelAsync();

        if (_executingTask is not null)
        {
#pragma warning disable VSTHRD003 // task was started by the timer callback, not this method
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
#pragma warning restore VSTHRD003
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
            await timer.DisposeAsync();

        _timer = null;
        _stoppingCts?.Dispose();
        _drainLock.Dispose();
    }

    private sealed class RetryGroup
    {
        public RetryGroup(string user, int anilistId, int episode, Models.Webhook.MediaType type)
        {
            User = user;
            AnilistId = anilistId;
            Episode = episode;
            Type = type;
        }

        public string User { get; }
        public int AnilistId { get; }
        public int Episode { get; set; }
        public Models.Webhook.MediaType Type { get; set; }
        public List<QueuedMutation> Entries { get; } = new();
    }
}
