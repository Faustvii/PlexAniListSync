using AniListNet;
using AniListNet.Objects;
using AniListNet.Parameters;

namespace PlexAniListSync.Services.AniList;

/// <summary>
/// Default <see cref="IAniClient"/> that forwards to the real <see cref="AniClient"/>.
/// </summary>
public class AniClientWrapper : IAniClient
{
    private readonly AniClient _client;

    public AniClientWrapper(AniClient client)
    {
        _client = client;
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
