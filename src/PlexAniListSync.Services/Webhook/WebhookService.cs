using Microsoft.Extensions.Logging;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.Mappings;

namespace PlexAniListSync.Services.Webhook;

public class WebhookService : IWebhookService
{
    private readonly ILogger<WebhookService> _logger;
    private readonly IMappingService _mappingService;
    private readonly IAniListService _aniListService;

    public WebhookService(ILogger<WebhookService> logger, IMappingService mappingService, IAniListService aniListService)
    {
        _logger = logger;
        _mappingService = mappingService;
        _aniListService = aniListService;
    }

    public async Task<bool> HandleAsync(WebhookData data)
    {
        _logger.LogWebhookUserWatched(data.User, data.ShowTitle, data.Episode, data.Season);
        var anilistId = _mappingService.GetAniListIdFromTitle(data.ShowTitle, data.Season, data.Episode);
        if (anilistId is default(int))
        {
            _logger.LogUnableToGetAnilistIdFromMappings(data.ShowTitle, data.Season);
            anilistId = (await _aniListService.FindShowAsync(data.ShowTitle, data.Season)).GetValueOrDefault();
            if (anilistId is default(int))
            {
                _logger.LogUnableToGetAnilistIdError(data.ShowTitle, data.Season);
                return false;
            }
        }
        var anilistEpisodeNumber = _mappingService.GetEpisodeNumber(data.Episode, anilistId);

        await _aniListService.UpdateShowAsync(data.User, anilistId, anilistEpisodeNumber);
        return true;
    }
}
