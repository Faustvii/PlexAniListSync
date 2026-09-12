using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.AniListNet;
using PlexAniListSync.Models.RetryQueue;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.Mappings;
using PlexAniListSync.Services.RetryQueue;

namespace PlexAniListSync.Services.Webhook;

public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly IMappingService _mappingService;
    private readonly IAniListService _aniListService;
    private readonly IMutationRetryQueue _retryQueue;
    private readonly IRateLimitGate _rateLimitGate;
    private readonly RetryQueueOptions _retryOptions;

    public WebhookService(
        ILogger<WebhookService> logger,
        IMappingService mappingService,
        IAniListService aniListService,
        IMutationRetryQueue retryQueue,
        IRateLimitGate rateLimitGate,
        IOptions<RetryQueueOptions> retryOptions
    )
    {
        _logger = logger;
        _mappingService = mappingService;
        _aniListService = aniListService;
        _retryQueue = retryQueue;
        _rateLimitGate = rateLimitGate;
        _retryOptions = retryOptions.Value;
    }

    public async Task<WebhookResult> HandleAsync(WebhookData data)
    {
        _logger.LogWebhookUserWatched(data.User, data.ShowTitle, data.Episode, data.Season, data.PlexGuid);

        var now = DateTimeOffset.UtcNow;
        if (_rateLimitGate.IsPaused(now))
        {
            return await QueueRateLimitedAsync(data, now);
        }

        try
        {
            var resolved = await ResolveAsync(data);
            if (resolved is null)
            {
                _logger.LogUnableToGetAnilistIdError(data.ShowTitle, data.Season);
                return WebhookResult.NotFound;
            }

            await _aniListService.UpdateMediaAsync(data.User, resolved.AnilistId, resolved.AnilistEpisode, resolved.Type);
            return WebhookResult.Applied;
        }
        catch (AniListRateLimitException ex)
        {
            _rateLimitGate.Pause(RetryScheduling.PauseUntil(ex, now, _retryOptions));
            return await QueueRateLimitedAsync(data, now);
        }
    }

    private async Task<WebhookResult> QueueRateLimitedAsync(WebhookData data, DateTimeOffset now)
    {
        var queued = await _retryQueue.EnqueueAsync(data, now, CancellationToken.None);
        if (!queued)
            return WebhookResult.RateLimited;

        _logger.LogMutationQueuedRateLimited(data.ShowTitle, data.Episode);
        return WebhookResult.Queued;
    }

    public async Task<ResolvedMutation?> ResolveAsync(WebhookData data)
    {
        var anilistId = _mappingService.GetAniListIdFromPlexGuid(data.PlexGuid, data.Season, data.Episode);
        if (anilistId is default(int))
        {
            _logger.LogUnableToGetAnilistIdFromMappings(data.ShowTitle, data.Season);
            anilistId = data.Type switch
            {
                MediaType.Movie => (await _aniListService.FindMovieAsync(data.ShowTitle)).GetValueOrDefault(),
                _ => (await _aniListService.FindShowAsync(data.ShowTitle, data.Season)).GetValueOrDefault()
            };
            if (anilistId is default(int))
                return null;
        }

        var (anilistEpisodeNumber, _) = data.Type switch
        {
            MediaType.Movie => (1, anilistId),
            _ => _mappingService.GetEpisodeNumber(data.Episode, anilistId)
        };

        return new ResolvedMutation(anilistId, anilistEpisodeNumber, data.Type);
    }
}
