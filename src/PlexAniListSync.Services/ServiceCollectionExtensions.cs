using AniListNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Plex.Api.Factories;
using Plex.Library.Factories;
using Plex.ServerApi;
using Plex.ServerApi.Api;
using Plex.ServerApi.Clients;
using Plex.ServerApi.Clients.Interfaces;
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
using PlexAniListSync.Services.Plex;
using PlexAniListSync.Services.Webhook;

namespace PlexAniListSync.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPlex(this IServiceCollection services, IConfiguration configuration)
    {
        var apiOptions = new ClientOptions
        {
            Product = "Plex Anilist Sync Service",
            DeviceName = "PlexAnilistSyncService",
            ClientId = "PlexAnilistSyncService",
            Platform = "Web",
            Version = "v1"
        };

        // Setup Dependency Injection

        services.AddSingleton(apiOptions);
        services.AddTransient<IPlexServerClient, PlexServerClient>();
        services.AddTransient<IPlexAccountClient, PlexAccountClient>();
        services.AddTransient<IPlexLibraryClient, PlexLibraryClient>();
        services.AddTransient<IApiService, ApiService>();
        services.AddTransient<IPlexFactory, PlexFactory>();
        services.AddTransient<IPlexRequestsHttpClient, PlexRequestsHttpClient>();
        services.AddTransient<IPlexWatchedService, PlexWatchedService>();
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
