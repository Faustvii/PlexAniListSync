using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlexAniListSync.Models.AniList;
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
using AniListNet;

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
        services.AddSingleton<AniClient>();
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

    public static IServiceCollection AddParsers(this IServiceCollection services)
    {
        services.AddTransient<IEpisodeRuleParser, EpisodeRuleParser>();
        services.AddTransient<IAnilistTVParser, AnilistTVParser>();
        return services;
    }
}
