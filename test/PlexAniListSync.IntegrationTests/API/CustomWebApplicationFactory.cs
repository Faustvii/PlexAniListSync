using System;
using AniListNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

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
        builder.ConfigureServices(_configureServices);
        builder.UseEnvironment("Development");
    }
}
