using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeoSmart.Caching.Sqlite;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using PlexAniListSync.Models.AniList;
using PlexAniListSync.Models.Cache;
using PlexAniListSync.Models.Mappings;
using PlexAniListSync.Models.Plex;
using PlexAniListSync.Services.AniList;
using PlexAniListSync.Services.Caching;
using PlexAniListSync.Services.Downloaders;
using PlexAniListSync.Services.Extractors;
using PlexAniListSync.Services.HostedServices;
using PlexAniListSync.Services.Mappings;
using PlexAniListSync.Services.Parsers;
using PlexAniListSync.Services.Webhook;
using PlexAniListSync.AniListNet;

namespace PlexAniListSync.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlex(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PlexOptions>(configuration.GetSection(PlexOptions.Key));
        return services;
    }

    public static IServiceCollection AddAnilist(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AniListOptions>(configuration.GetSection(AniListOptions.Key));
        services.AddHttpClient<AniClient>();
        services.AddSingleton<IAniClient, AniClientWrapper>();
        services.AddTransient<IAniListService, AniListService>();
        return services;
    }

    public static IServiceCollection AddWebhooks(this IServiceCollection services)
    {
        services.AddTransient<IWebhookService, WebhookService>();
        return services;
    }

    public static IServiceCollection AddExtractor(this IServiceCollection services)
    {
        services.AddSingleton<IExtractor, Extractor>();
        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<IDownloadService, DownloadService>();
        return services;
    }

    public static IServiceCollection AddHostedServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SourceOptions>(configuration.GetSection(SourceOptions.Key));
        services.AddHostedService<PeriodicDownloaderService>();
        return services;
    }

    public static IServiceCollection AddMappingServices(this IServiceCollection services)
    {
        services.AddTransient<IMappingService, MappingService>();
        return services;
    }

    public static IServiceCollection AddDataCache(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddTransient<IDataCache, DataCache>();
        return services;
    }

    public static IServiceCollection AddResponseCache(this IServiceCollection services, IConfiguration configuration)
    {
        var cacheOptions = configuration.GetSection(CacheOptions.Key).Get<CacheOptions>() ?? new CacheOptions();
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.Key));

        var fusionCache = services
            .AddFusionCache()
            .WithDefaultEntryOptions(options =>
            {
                options.Duration = TimeSpan.FromHours(cacheOptions.LookupTtlHours);
                // Serve a stale value if AniList is down / rate-limiting rather than failing the webhook.
                options.IsFailSafeEnabled = true;
                options.FailSafeMaxDuration = TimeSpan.FromDays(7);
                options.FactorySoftTimeout = TimeSpan.FromSeconds(10);
            });

        switch (cacheOptions.Backend)
        {
            case CacheBackend.Sqlite:
                var sqlitePath = cacheOptions.SqlitePath;
                var directory = Path.GetDirectoryName(sqlitePath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                services.AddSqliteCache(options => options.CachePath = sqlitePath);
                fusionCache
                    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                    .WithRegisteredDistributedCache();
                break;
            case CacheBackend.Redis:
                services.AddStackExchangeRedisCache(options => options.Configuration = cacheOptions.RedisConnection);
                fusionCache
                    .WithSerializer(new FusionCacheSystemTextJsonSerializer())
                    .WithRegisteredDistributedCache();
                break;
            case CacheBackend.Memory:
            default:
                // L1 only - no persistent backend.
                break;
        }

        return services;
    }

    public static IServiceCollection AddParsers(this IServiceCollection services)
    {
        services.AddTransient<IEpisodeRuleParser, EpisodeRuleParser>();
        services.AddTransient<IAnilistTVParser, AnilistTVParser>();
        return services;
    }
}
