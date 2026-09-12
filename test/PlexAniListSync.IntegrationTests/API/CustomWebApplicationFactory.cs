using System;
using PlexAniListSync.AniListNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using ZiggyCreatures.Caching.Fusion;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly Action<IServiceCollection> _configureServices;

    public CustomWebApplicationFactory(Action<IServiceCollection>? configureServices = null)
    {
        if (configureServices is null)
        {
            _configureServices = services =>
            {
                services.RemoveAll<AniClient>();
                services.AddSingleton(Mock.Of<AniClient>(MockBehavior.Loose));
            };
        }
        else
        {
            _configureServices = configureServices;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Keep tests hermetic: replace the configured FusionCache (whose SQLite L2 would persist
        // a cache.db across runs) with a plain in-memory (L1-only) instance. Done at the DI layer
        // because a ConfigureAppConfiguration override lands too late - AddResponseCache reads the
        // Cache section while Program.cs runs, before the test host's config callbacks apply.
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFusionCache>();
            services.AddSingleton<IFusionCache>(new FusionCache(Options.Create(new FusionCacheOptions())));
        });
        builder.ConfigureServices(_configureServices);
        builder.UseEnvironment("Development");
    }
}
