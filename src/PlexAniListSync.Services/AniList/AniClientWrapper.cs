using AniListNet;
using AniListNet.Objects;
using AniListNet.Parameters;
using Microsoft.Extensions.Logging;

namespace PlexAniListSync.Services.AniList;

/// <summary>
/// Default <see cref="IAniClient"/> that forwards to the real <see cref="AniClient"/>.
/// Registered as a singleton, so it owns the (single) rate-limit log subscription.
/// </summary>
public class AniClientWrapper : IAniClient
{
    private readonly AniClient _client;
    private readonly ILogger<AniClientWrapper> _logger;

    public AniClientWrapper(AniClient client, ILogger<AniClientWrapper> logger)
    {
        _client = client;
        _logger = logger;
        _client.RateChanged += RateLimitHandler;
    }

    private void RateLimitHandler(object? sender, AniRateEventArgs eventArgs)
    {
        _logger.LogAnilistRatelimit(eventArgs.RateRemaining);
    }

    public event EventHandler<AniRateEventArgs>? RateChanged
    {
        add => _client.RateChanged += value;
        remove => _client.RateChanged -= value;
    }

    public Task<bool> TryAuthenticateAsync(string token) => _client.TryAuthenticateAsync(token);

    public Task<MediaEntry?> GetMediaEntryAsync(int mediaId) => _client.GetMediaEntryAsync(mediaId);

    public Task<MediaEntry> SaveMediaEntryAsync(int mediaId, MediaEntryMutation mutation) =>
        _client.SaveMediaEntryAsync(mediaId, mutation);

    public Task<AniPagination<Media>> SearchMediaAsync(SearchMediaFilter filter) => _client.SearchMediaAsync(filter);
}
