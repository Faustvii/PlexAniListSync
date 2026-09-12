using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using AniListNet;
using AniListNet.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Cache;
using ZiggyCreatures.Caching.Fusion;
using static PlexAniListSync.Models.AniList.AniListOptions;
using MediaType = AniListNet.Objects.MediaType;

namespace PlexAniListSync.Services.AniList;

public class AniListService : IAniListService
{
    private readonly IAniClient _client;
    private readonly IOptions<AniListOptions> _options;
    private readonly ILogger<AniListService> _logger;
    private readonly IFusionCache _cache;
    private readonly CacheOptions _cacheOptions;
    private TimeSpan LookupTtl => TimeSpan.FromHours(_cacheOptions.LookupTtlHours);
    private TimeSpan NotFoundTtl => TimeSpan.FromHours(_cacheOptions.NotFoundTtlHours);
    private TimeSpan MediaEntryTtl => TimeSpan.FromHours(_cacheOptions.MediaEntryTtlHours);

    public AniListService(
        IOptions<AniListOptions> options,
        ILogger<AniListService> logger,
        IAniClient client,
        IFusionCache cache,
        IOptions<CacheOptions> cacheOptions
    )
    {
        _options = options;
        _logger = logger;
        _client = client;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _client.RateChanged += RateLimitHandler;
    }

    public ValueTask<int?> FindShowAsync(string title, int season)
    {
        var key = $"anilist:show:{NormalizeTitle(title)}:{season.ToStringInvariantCulture()}";
        return _cache.GetOrSetAsync<int?>(
            key,
            (ctx, _) => FindShowUncachedAsync(title, season, ctx),
            options => options.SetDuration(LookupTtl)
        );
    }

    public ValueTask<int?> FindMovieAsync(string title)
    {
        var key = $"anilist:movie:{NormalizeTitle(title)}";
        return _cache.GetOrSetAsync<int?>(
            key,
            (ctx, _) => FindMovieUncachedAsync(title, ctx),
            options => options.SetDuration(LookupTtl)
        );
    }

    private async Task<int?> FindShowUncachedAsync(
        string title,
        int season,
        FusionCacheFactoryExecutionContext<int?> ctx
    )
    {
        var mediaPage = await QueryForShowAsync(title, season);
        int? id;
        if (mediaPage.Data.Length != 1)
        {
            _logger.LogUnexpectedAmoutOfShows(mediaPage.Data.Length);
            var exactMatch = RetrieveExactMatchMedia(title, mediaPage, season);
            if (exactMatch is not null)
                _logger.LogOnlyOneShowMatchedExactTitle(title, mediaPage.Data.Select(x => x.Title.PreferredTitle));
            id = exactMatch?.Id;
        }
        else
        {
            id = mediaPage.Data[0].Id;
        }

        ctx.Options.SetDuration(id.HasValue ? LookupTtl : NotFoundTtl);
        return id;
    }

    private async Task<int?> FindMovieUncachedAsync(string title, FusionCacheFactoryExecutionContext<int?> ctx)
    {
        var mediaPage = await QueryForMovieAsync(title);
        int? id;
        if (mediaPage.Data.Length != 1)
        {
            _logger.LogUnexpectedAmoutOfShows(mediaPage.Data.Length);
            var exactMatch = RetrieveExactMatchMedia(title, mediaPage);
            if (exactMatch is not null)
                _logger.LogOnlyOneShowMatchedExactTitle(title, mediaPage.Data.Select(x => x.Title.PreferredTitle));
            id = exactMatch?.Id;
        }
        else
        {
            id = mediaPage.Data[0].Id;
        }

        ctx.Options.SetDuration(id.HasValue ? LookupTtl : NotFoundTtl);
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

        var entryCacheKey = MediaEntryCacheKey(user.Token, anilistId);
        var mediaEntry = await _cache.GetOrSetAsync<CachedMediaEntry?>(
            entryCacheKey,
            async (_, _) =>
            {
                var entry = await _client.GetMediaEntryAsync(anilistId);
                return entry is null ? null : CachedMediaEntry.From(entry);
            },
            options => options.SetDuration(MediaEntryTtl)
        );

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
            StartDate = mediaEntry is null ? startDate : mediaEntry.StartDate,
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
        {
            var saved = await _client.SaveMediaEntryAsync(anilistId, mutation);
            // Write-through: keep the cached progress fresh so a binge's next episode
            // reads from cache instead of hitting AniList again.
            await _cache.SetAsync(entryCacheKey, CachedMediaEntry.From(saved), options => options.SetDuration(MediaEntryTtl));
        }
    }

    internal static string NormalizeTitle(string title)
    {
        var normalized = RemoveAnnoyingCharacters(title).Trim().ToLowerInvariant();
        // collapse internal whitespace runs so "a  b" and "a b" share a key
        return string.Join(' ', normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    // AniList tokens are secrets; never put the raw token in a cache key (it can land in the SQLite/Redis store).
    internal static string MediaEntryCacheKey(string token, int anilistId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hash = Convert.ToHexString(bytes);
        return $"anilist:entry:{hash}:{anilistId.ToStringInvariantCulture()}";
    }

    private void RateLimitHandler(object? sender, AniRateEventArgs eventArgs)
    {
        _logger.LogAnilistRatelimit(eventArgs.RateRemaining);
    }
}
