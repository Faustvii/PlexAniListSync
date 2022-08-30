using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.Webhook;

public interface IWebhookService
{
    Task<bool> HandleAsync(WebhookData data);
}
