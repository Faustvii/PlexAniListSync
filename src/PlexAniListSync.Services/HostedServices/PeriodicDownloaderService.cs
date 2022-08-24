using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlexAniListSync.Models.Mappings;
using PlexAniListSync.Services.Caching;
using PlexAniListSync.Services.Downloaders;
using PlexAniListSync.Services.Parsers;

namespace PlexAniListSync.Services.HostedServices;

public sealed class PeriodicDownloaderService : IHostedService, IAsyncDisposable
{
    private readonly ILogger<PeriodicDownloaderService> _logger;
    private readonly IDownloadService _downloadService;
    private readonly IDataCache _cache;
    private readonly IEpisodeRuleParser _episodeRuleParser;
    private readonly IAnilistTVParser _anilistTVParser;
    private readonly IOptions<SourceOptions> _optionsAccessor;
    private Timer? _timer;

    public PeriodicDownloaderService(
        ILogger<PeriodicDownloaderService> logger,
        IDownloadService downloadService,
        IDataCache cache,
        IEpisodeRuleParser episodeRuleParser,
        IAnilistTVParser anilistTVParser,
        IOptions<SourceOptions> optionsAccessor)
    {
        _logger = logger;
        _downloadService = downloadService;
        _cache = cache;
        _episodeRuleParser = episodeRuleParser;
        _anilistTVParser = anilistTVParser;
        _optionsAccessor = optionsAccessor;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogHostedServiceStarting(nameof(PeriodicDownloaderService));
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(_optionsAccessor.Value.CheckForUpdateEveryHours));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        var options = _optionsAccessor.Value;

        var episodeRuleMappings = await GetEpisodeRuleMappings(options.EpisodeRuleUrls);
        _cache.SetEpisodeRuleMappings(episodeRuleMappings);

        var anilistMappings = await GetAnilistMappings(options.AnilistMappingUrls);
        _cache.SetAnilistMappings(anilistMappings);
    }

    private async Task<IReadOnlyList<EpisodeRuleMapping>> GetEpisodeRuleMappings(string[] sourceUrls)
    {
        var episodeRuleMappings = new List<EpisodeRuleMapping>();
        foreach (var sourceUrl in sourceUrls)
        {
            var animeEpisodeRuleContent = await _downloadService.DownloadAsync(sourceUrl);
            episodeRuleMappings.AddRange(_episodeRuleParser.ParseRules(animeEpisodeRuleContent));
        }

        return episodeRuleMappings;
    }

    private async Task<IReadOnlyList<AnilistMapping>> GetAnilistMappings(string[] sourceUrls)
    {
        var anilistMappings = new List<AnilistMapping>();
        foreach (var sourceUrl in sourceUrls)
        {
            var anilistMappingContent = await _downloadService.DownloadAsync(sourceUrl);
            anilistMappings.AddRange(_anilistTVParser.ParseMappings(anilistMappingContent));
        }

        return anilistMappings;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogHostedServiceStopping(nameof(PeriodicDownloaderService));
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
        {
            await timer.DisposeAsync();
        }

        _timer = null;
    }
}
