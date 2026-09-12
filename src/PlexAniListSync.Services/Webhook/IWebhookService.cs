using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.Webhook;

public interface IWebhookService
{
    Task<WebhookResult> HandleAsync(WebhookData data);

    Task<ResolvedMutation?> ResolveAsync(WebhookData data);
}
