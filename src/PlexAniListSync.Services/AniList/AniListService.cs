using System.Diagnostics.CodeAnalysis;
using AniListNet;
using AniListNet.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.AniList;
using static PlexAniListSync.Models.AniList.AniListOptions;
using MediaType = AniListNet.Objects.MediaType;

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
        var mediaPage = await QueryForShowAsync(title, season);
        if (mediaPage.Data.Length != 1)
        {
            _logger.LogUnexpectedAmoutOfShows(mediaPage.Data.Length);
            var exactMatch = RetrieveExactMatchMedia(title, mediaPage, season);
            if (exactMatch is not null)
            {
                _logger.LogOnlyOneShowMatchedExactTitle(title, mediaPage.Data.Select(x => x.Title.PreferredTitle));
                return exactMatch.Id;
            }

            return null;
        }

        var id = mediaPage.Data[0].Id;
        return id;
    }

    public async Task<int?> FindMovieAsync(string title)
    {
        var mediaPage = await QueryForMovieAsync(title);
        if (mediaPage.Data.Length != 1)
        {
            _logger.LogUnexpectedAmoutOfShows(mediaPage.Data.Length);
            var exactMatch = RetrieveExactMatchMedia(title, mediaPage);
            if (exactMatch is not null)
            {
                _logger.LogOnlyOneShowMatchedExactTitle(title, mediaPage.Data.Select(x => x.Title.PreferredTitle));
                return exactMatch.Id;
            }

            return null;
        }

        var id = mediaPage.Data[0].Id;
        return id;
    }

    private Media? RetrieveExactMatchMedia(string title, AniPagination<Media> media, int season = 1)
    {
        if (media.Data.Where(x => TitleMatchesExactlyIgnoreCase(x, title, season)).Take(2).Count() == 1)
        {
            return media.Data.SingleOrDefault(x => TitleMatchesExactlyIgnoreCase(x, title, season));
        }

        return null;
    }

    private static bool TitleMatchesExactlyIgnoreCase(Media media, string title, int season = 1)
    {
        var safeTitle = RemoveAnnoyingCharacters(title);
        var candidates = BuildSeasonTitleCandidates(safeTitle, season);

        bool MatchesAny(string? mediaTitle) =>
            mediaTitle is not null
            && candidates.Contains(RemoveAnnoyingCharacters(mediaTitle), StringComparer.OrdinalIgnoreCase);

        return MatchesAny(media.Title.EnglishTitle)
            || MatchesAny(media.Title.RomajiTitle)
            || MatchesAny(media.Title.PreferredTitle)
            || media.Synonyms.Any(MatchesAny);
    }

    // AniList has no sequel-index field, so for season > 1 we only accept titles carrying a
    // sequel marker ("Season 2", "II", ...) - a bare title match would just be the first season.
    private static IReadOnlyCollection<string> BuildSeasonTitleCandidates(string title, int season)
    {
        if (season <= 1)
            return new[] { title };

        var seasonText = season.ToStringInvariantCulture();
        return new[]
        {
            $"{title} {seasonText}",
            $"{title} Season {seasonText}",
            $"{title} Part {seasonText}",
            $"{title} {ToOrdinal(season)} Season",
            $"{title} {ToRomanNumeral(season)}",
        };
    }

    private static string ToOrdinal(int number) =>
        (number % 100) switch
        {
            11 or 12 or 13 => $"{number}th",
            _ => (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            }
        };

    private static readonly string[] RomanNumerals =
    {
        "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X"
    };

    private static string ToRomanNumeral(int number) =>
        number >= 1 && number <= RomanNumerals.Length ? RomanNumerals[number - 1] : number.ToStringInvariantCulture();

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
            1 => title,
            2 => $"{title} season {season.ToStringInvariantCulture()}",
            3 => $"{title} {season.ToStringInvariantCulture()}",
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

        var mediaPage = await _client.SearchMediaAsync(filter);
        if (mediaPage.Data.Length != 1)
        {
            // Let's see if only one show matches the exact title, if so, we can return it
            var exactMatch = RetrieveExactMatchMedia(title, mediaPage, season);
            if (exactMatch is not null)
            {
                return mediaPage;
            }
        }
        if (mediaPage.Data.Length != 1 && level < 3)
        {
            _logger.LogNoShowFound(filter.Query);
            mediaPage = await QueryForShowAsync(title, season, level + 1);
        }

        return mediaPage;
    }

    private async Task<AniPagination<Media>> QueryForMovieAsync(string title, int level = 1)
    {
        title = RemoveCharactersThatRequiresEscaping(title);
        var query = title;
        var format = level switch
        {
            1 => MediaFormat.Movie,
            2 => MediaFormat.Special,
            _ => MediaFormat.Movie
        };

        var filter = new AniListNet.Parameters.SearchMediaFilter
        {
            Type = MediaType.Anime,
            Format = new Dictionary<MediaFormat, bool> { { format, true } },
            Status = new Dictionary<MediaStatus, bool> { { MediaStatus.NotYetReleased, false }, },
            Query = query,
        };

        var mediaPage = await _client.SearchMediaAsync(filter);
        if (mediaPage.Data.Length != 1)
        {
            // Let's see if only one show matches the exact title, if so, we can return it
            var exactMatch = RetrieveExactMatchMedia(title, mediaPage);
            if (exactMatch is not null)
            {
                return mediaPage;
            }
        }
        if (mediaPage.Data.Length != 1 && level < 2)
        {
            _logger.LogNoShowFound(filter.Query);
            mediaPage = await QueryForMovieAsync(title, level + 1);
        }

        return mediaPage;
    }

    public async Task UpdateMediaAsync(
        string plexUsername,
        int anilistId,
        int episode,
        Models.Webhook.MediaType mediaType
    )
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
            await UpdateMediaForAnilistUserAsync(user, anilistId, episode, mediaType);
        }
    }

    private async Task UpdateMediaForAnilistUserAsync(
        AniListUser user,
        int anilistId,
        int episode,
        Models.Webhook.MediaType mediaType
    )
    {
        var authenticated = await _client.TryAuthenticateAsync(user.Token);
        if (authenticated is false)
        {
            _logger.LogUnableToAuthenticateAnilist(anilistId, episode);
            return;
        }

        var status = mediaType switch
        {
            Models.Webhook.MediaType.Movie => MediaEntryStatus.Completed,
            _ => MediaEntryStatus.Current,
        };

        var mediaEntry = await _client.GetMediaEntryAsync(anilistId);
        DateTime? completedDate = mediaType switch
        {
            Models.Webhook.MediaType.Movie => DateTime.UtcNow,
            _ => null,
        };
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
