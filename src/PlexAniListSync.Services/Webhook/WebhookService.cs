using Microsoft.Extensions.Logging;
using PlexAniListSync.Models.Webhook;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.Extractors;
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

    public async Task<bool> Handle(WebhookData data)
    {
        var anilistId = _mappingService.GetAniListIdFromTitle(data.ShowTitle, data.Season);
        if (anilistId is default(int))
        {
            _logger.LogInformation("We were unable to get AniListId from {ShowTitle} - {Season} from mapping files", data.ShowTitle, data.Season);
            anilistId = (await _aniListService.FindShow(data.ShowTitle, data.Season)).GetValueOrDefault();
            if (anilistId is default(int))
            {
                _logger.LogError("We were unable to get AniListId from AniList for {ShowTitle}S{Season} - Aborting", data.ShowTitle, data.Season);
                return false;
            }
        }
        var anilistEpisodeNumber = _mappingService.GetEpisodeNumber(data.Episode, anilistId);

        await _aniListService.UpdateShow(anilistId, anilistEpisodeNumber);
        return true;
    }
}
