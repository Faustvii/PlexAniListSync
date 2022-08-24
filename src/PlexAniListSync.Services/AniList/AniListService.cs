using AniListNet;
using AniListNet.Objects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.AniList;

namespace PlexAniListSync.Services.AniList;

public class AniListService : IAniListService
{
    private readonly AniClient _client = new();
        private readonly IOptions<AniListOptions> _options;
        private readonly ILogger<AniListService> _logger;

        public AniListService(IOptions<AniListOptions> options, ILogger<AniListService> logger)
        {
            _options = options;
            _logger = logger;
            _client.RateChanged += rateLimitHandler;
        }

        public async Task<int?> FindShow(string title, int season)
        {
            var media = await QueryForShow(title, season);
            if (media.Data.Length != 1)
            {
                _logger.LogWarning("We got {Scenario} results trying to search by title", media.Data.Length > 1 ? "multiple" : "no");
                return null;
            }

            var id = media.Data.First().Id;
            return id;
        }

        private async Task<AniPagination<Media>> QueryForShow(string title, int season, int level = 1)
        {

            var query = level switch
            {
                1 => $"{title} season {season}",
                2 => $"{title} {season}",
                3 => title,
                _ => title
            };

            var filter = new AniListNet.Parameters.SearchMediaFilter
            {
                Type = MediaType.Anime,
                Format = new Dictionary<MediaFormat, bool>
                    {
                        {MediaFormat.TV, true},
                        {MediaFormat.TVShort, true},
                        {MediaFormat.Special, true},
                    },
                Query = query,
            };

            var media = await _client.SearchMediaAsync(filter);
            if (media.Data.Length == 0 && level < 3)
            {
                _logger.LogInformation("We got no matches with '{Query}' - Let's try a bit less specific", filter.Query);
                media = await QueryForShow(title, season, level + 1);
            }

            return media;
        }

        public async Task UpdateShow(int anilistId, int episode)
        {
            if (!_client.IsAuthenticated)
            {
                var authenticated = await _client.TryAuthenticateAsync(_options.Value.Token);
                if (authenticated is false)
                {
                    _logger.LogError("We were unable to authenticate {AnilistId} {Episode} was not updated", anilistId, episode);
                    return;
                }
            }
            var status = MediaEntryStatus.Current;
            var mediaEntry = await _client.GetMediaEntryAsync(anilistId);
            var completedDate = (DateTime?)null;
            var startDate = DateTime.UtcNow;
            if (mediaEntry is not null)
            {
                if (mediaEntry.Progress >= episode)
                {
                    _logger.LogWarning("Anilist has {MaxProgress} episodes watched and we just watched {Episode} episode (Are we rewatching?) - Skipping update", mediaEntry.Progress, episode);
                    return;
                }
                if (mediaEntry.MaxProgress == episode)
                {
                    status = MediaEntryStatus.Completed;
                    completedDate = DateTime.UtcNow;
                }
            }

            var mutation = new AniListNet.Parameters.MediaEntryMutation(mediaEntry)
            {
                Progress = episode,
                Status = status,
                StartDate = mediaEntry is null ? startDate : mediaEntry.StartDate.ToDateTime(),
                CompleteDate = completedDate,
            };

            _logger.LogInformation("TestMode: '{TestMode}' Updating Anilist {AniListId} {Episode} out of {MaxEpisodes} - Status '{Status}'", _options.Value.TestMode, anilistId, mutation.Progress, mediaEntry?.MaxProgress, status);

            if (_options.Value.TestMode is false)
                await _client.SaveMediaEntryAsync(anilistId, mutation);
        }

        private void rateLimitHandler(object? sender, AniRateEventArgs eventArgs)
        {
            _logger.LogInformation("Ratelimit left {RateRemaining}", eventArgs.RateRemaining);
        }
    }
}
