using AniListNet;
using AniListNet.Objects;
using AniListNet.Parameters;

namespace PlexAniListSync.Services.AniList;

/// <summary>
/// Thin seam over <see cref="AniClient"/> exposing only the members this app uses.
/// Exists so the AniList calls can be mocked in tests and cached at the boundary.
/// </summary>
public interface IAniClient
{
    event EventHandler<AniRateEventArgs>? RateChanged;

    Task<bool> TryAuthenticateAsync(string token);

    Task<MediaEntry?> GetMediaEntryAsync(int mediaId);

    Task<MediaEntry> SaveMediaEntryAsync(int mediaId, MediaEntryMutation mutation);

    Task<AniPagination<Media>> SearchMediaAsync(SearchMediaFilter filter);
}
