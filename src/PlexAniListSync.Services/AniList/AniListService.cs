using System.Diagnostics.CodeAnalysis;
using AniListNet;
using AniListNet.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.AniList;
using static PlexAniListSync.Models.AniList.AniListOptions;

namespace PlexAniListSync.Services.AniList;

public class AniListService : IAniListService
{
    private readonly AniClient _client;
    private readonly IOptions<AniListOptions> _options;
    private readonly ILogger<AniListService> _logger;

    public AniListService(IOptions<AniListOptions> options, ILogger<AniListService> logger, AniClient client)
    {
        _options = options;
        _logger = logger;
        _client = client;
        _client.RateChanged += RateLimitHandler;
    }

    public async Task<int?> FindShowAsync(string title, int season)
    {
        var media = await QueryForShowAsync(title, season);
        if (media.Data.Length != 1)
        {
            _logger.LogUnexpectedAmoutOfShows(media.Data.Length);
            if (media.Data.Where(x => TitleMatchesExactlyIgnoreCase(x, title)).Take(2).Count() == 1)
            {
                _logger.LogOnlyOneShowMatchedExactTitle(title, media.Data.Select(x => x.Title.PreferredTitle));
                return media.Data.SingleOrDefault(x => TitleMatchesExactlyIgnoreCase(x, title))?.Id;
            }

            return null;
        }

        var id = media.Data[0].Id;
        return id;
    }

    private static bool TitleMatchesExactlyIgnoreCase(Media media, string title)
    {
        var safeTitle = RemoveAnnoyingCharacters(title);
        return safeTitle.Equals(RemoveAnnoyingCharacters(media.Title.EnglishTitle), StringComparison.OrdinalIgnoreCase)
            || safeTitle.Equals(RemoveAnnoyingCharacters(media.Title.RomajiTitle), StringComparison.OrdinalIgnoreCase)
            || safeTitle.Equals(
                RemoveAnnoyingCharacters(media.Title.PreferredTitle),
                StringComparison.OrdinalIgnoreCase
            )
            || safeTitle.Equals(RemoveAnnoyingCharacters(media.Title.RomajiTitle), StringComparison.OrdinalIgnoreCase)
            || media.Synonyms.Any(
                x => safeTitle.Equals(RemoveAnnoyingCharacters(x), StringComparison.OrdinalIgnoreCase)
            );
    }

    [return: NotNullIfNotNull("title")]
    private static string? RemoveAnnoyingCharacters(string? title)
    { // Plex might send ' or ` instead of ' and AniList might use either
        // so we remove them before compare
        if (title is null)
            return null;

        return title
            .Replace("’", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("`", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("'", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveCharactersThatRequiresEscaping(string title)
    {
        return title.Replace("\"", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<AniPagination<Media>> QueryForShowAsync(string title, int season, int level = 1)
    {
        title = RemoveCharactersThatRequiresEscaping(title);
        var query = level switch
        {
            1 => $"{title} season {season.ToStringInvariantCulture()}",
            2 => $"{title} {season.ToStringInvariantCulture()}",
            3 => title,
            _ => title
        };

        var filter = new AniListNet.Parameters.SearchMediaFilter
        {
            Type = MediaType.Anime,
            Format = new Dictionary<MediaFormat, bool>
            {
                { MediaFormat.TV, true },
                { MediaFormat.TVShort, true },
                { MediaFormat.Special, true },
                { MediaFormat.ONA, true },
            },
            Status = new Dictionary<MediaStatus, bool> { { MediaStatus.NotYetReleased, false }, },
            Query = query,
        };

        var media = await _client.SearchMediaAsync(filter);
        if (media.Data.Length != 1 && level < 3)
        {
            _logger.LogNoShowFound(filter.Query);
            media = await QueryForShowAsync(title, season, level + 1);
        }

        return media;
    }

    public async Task UpdateShowAsync(string plexUsername, int anilistId, int episode)
    {
        var users = _options.Value.Users.Where(
            x => x.PlexUsernames.Any(t => t.Equals(plexUsername, StringComparison.OrdinalIgnoreCase))
        );
        if (!users.Any())
        {
            _logger.LogUnableToFindTokenFromPlexUser(plexUsername);
            return;
        }

        foreach (var user in users)
        {
            await UpdateShowForAnilistUserAsync(user, anilistId, episode);
        }
    }

    private async Task UpdateShowForAnilistUserAsync(AniListUser user, int anilistId, int episode)
    {
        var authenticated = await _client.TryAuthenticateAsync(user.Token);
        if (authenticated is false)
        {
            _logger.LogUnableToAuthenticateAnilist(anilistId, episode);
            return;
        }

        var status = MediaEntryStatus.Current;
        var mediaEntry = await _client.GetMediaEntryAsync(anilistId);
        var completedDate = (DateTime?)null;
        var startDate = DateTime.UtcNow;
        if (mediaEntry is not null)
        {
            if (mediaEntry.Progress >= episode)
            {
                _logger.LogAnilistUpdateSkipped(mediaEntry.Progress, episode);
                return;
            }
            if (mediaEntry.MaxProgress == episode)
            {
                status = MediaEntryStatus.Completed;
                completedDate = DateTime.UtcNow;
            }
        }

        var mutation = new AniListNet.Parameters.MediaEntryMutation()
        {
            Progress = episode,
            Status = status,
            StartDate = mediaEntry is null ? startDate : mediaEntry.StartDate.ToDateTime(),
            CompleteDate = completedDate,
        };

        _logger.LogAnilistUpdate(
            _options.Value.TestMode,
            anilistId,
            mutation.Progress,
            mediaEntry?.MaxProgress,
            status
        );

        if (_options.Value.TestMode is false)
            await _client.SaveMediaEntryAsync(anilistId, mutation);
    }

    private void RateLimitHandler(object? sender, AniRateEventArgs eventArgs)
    {
        _logger.LogAnilistRatelimit(eventArgs.RateRemaining);
    }
}
