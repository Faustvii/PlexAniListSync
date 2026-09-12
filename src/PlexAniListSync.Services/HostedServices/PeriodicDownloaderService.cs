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
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public PeriodicDownloaderService(
        ILogger<PeriodicDownloaderService> logger,
        IDownloadService downloadService,
        IDataCache cache,
        IEpisodeRuleParser episodeRuleParser,
        IAnilistTVParser anilistTVParser,
        IOptions<SourceOptions> optionsAccessor
    )
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
        _stoppingCts = new CancellationTokenSource();
        _timer = new Timer(
            TimerCallback,
            state: null,
            TimeSpan.Zero,
            TimeSpan.FromHours(_optionsAccessor.Value.CheckForUpdateEveryHours)
        );
        return Task.CompletedTask;
    }

    // We need void because Timer expects a void callback
    private void TimerCallback(object? state)
    {
        _executingTask = DoWorkAsync(_stoppingCts!.Token);
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        try
        {
            var options = _optionsAccessor.Value;

            var episodeRuleMappings = await GetEpisodeRuleMappingsAsync(options.EpisodeRuleUrls);
            cancellationToken.ThrowIfCancellationRequested();
            _cache.SetEpisodeRuleMappings(episodeRuleMappings);

            var anilistMappings = await GetAnilistMappingsAsync(options.AnilistMappingUrls);
            cancellationToken.ThrowIfCancellationRequested();
            _cache.SetAnilistMappings(anilistMappings);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down, discard the in-flight result instead of writing to a disposed cache.
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedHostedServiceError(nameof(PeriodicDownloaderService), ex);
        }
    }

    private async Task<IReadOnlyList<EpisodeRuleMapping>> GetEpisodeRuleMappingsAsync(string[] sourceUrls)
    {
        var episodeRuleMappings = new List<EpisodeRuleMapping>();
        foreach (var sourceUrl in sourceUrls)
        {
            var animeEpisodeRuleContent = await _downloadService.DownloadAsync(sourceUrl);
            episodeRuleMappings.AddRange(_episodeRuleParser.ParseRules(animeEpisodeRuleContent));
        }

        return episodeRuleMappings;
    }

    private async Task<IReadOnlyList<AnilistMapping>> GetAnilistMappingsAsync(string[] sourceUrls)
    {
        var anilistMappings = new List<AnilistMapping>();
        foreach (var sourceUrl in sourceUrls)
        {
            var anilistMappingContent = await _downloadService.DownloadAsync(sourceUrl);
            anilistMappings.AddRange(_anilistTVParser.ParseMappings(anilistMappingContent));
        }

        return anilistMappings;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogHostedServiceStopping(nameof(PeriodicDownloaderService));
        _timer?.Change(Timeout.Infinite, 0);
        if (_stoppingCts is not null)
        {
            await _stoppingCts.CancelAsync();
        }

        // Wait for any in-flight download/cache-write to finish (or observe cancellation) before
        // the host disposes the DI container - otherwise it can write to an already-disposed cache.
        if (_executingTask is not null)
        {
#pragma warning disable VSTHRD003 // task was started by the timer callback, not this method
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
#pragma warning restore VSTHRD003
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_timer is IAsyncDisposable timer)
        {
            await timer.DisposeAsync();
        }

        _timer = null;
        _stoppingCts?.Dispose();
    }
}
