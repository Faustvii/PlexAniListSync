using PlexAniListSync.Models.Webhook;

namespace PlexAniListSync.Services.Webhook;

public sealed record ResolvedMutation(int AnilistId, int AnilistEpisode, MediaType Type);
