using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.Webhook;

public interface IWebhookService
{
    Task<bool> Handle(WebhookData data);
}
