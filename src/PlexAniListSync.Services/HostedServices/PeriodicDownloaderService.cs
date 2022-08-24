using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    private Timer? _timer;

    public PeriodicDownloaderService(
        ILogger<PeriodicDownloaderService> logger,
        IDownloadService downloadService,
        IDataCache cache,
        IEpisodeRuleParser episodeRuleParser,
        IAnilistTVParser anilistTVParser)
    {
        _logger = logger;
        _downloadService = downloadService;
        _cache = cache;
        _episodeRuleParser = episodeRuleParser;
        _anilistTVParser = anilistTVParser;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogHostedServiceStarting(nameof(PeriodicDownloaderService));
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(6));
        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        var animeEpisodeRuleContent = await _downloadService.DownloadAsync("https://raw.githubusercontent.com/erengy/anime-relations/master/anime-relations.txt");
        var episodeRuleMappings = _episodeRuleParser.ParseRules(animeEpisodeRuleContent);
        _cache.SetEpisodeRuleMappings(episodeRuleMappings);
        var anilistMappingContent = await _downloadService.DownloadAsync("https://raw.githubusercontent.com/RickDB/PlexAniSync-Custom-Mappings/main/series-tvdb.en.yaml");
        var anilistMappings = _anilistTVParser.ParseMappings(anilistMappingContent);
        _cache.SetAnilistMappings(anilistMappings);
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
