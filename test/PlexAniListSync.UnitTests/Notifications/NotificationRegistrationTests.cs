using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PlexAniListSync.Models.Notifications;
using PlexAniListSync.Services;
using PlexAniListSync.Services.Notifications;
using Xunit;

namespace PlexAniListSync.UnitTests.Notifications;

public class NotificationRegistrationTests
{
    private static ServiceProvider BuildServices(params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(x => x.Key, x => (string?)x.Value))
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddNotifications(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Disabled_RegistersNoOpAndNoWorkerOrSink()
    {
        using var provider = BuildServices(("Notifications:Enabled", "false"));

        Assert.IsType<NoOpNotificationService>(provider.GetRequiredService<INotificationService>());
        Assert.DoesNotContain(provider.GetServices<ILoggerProvider>(), x => x is ErrorNotificationLoggerProvider);
        Assert.DoesNotContain(provider.GetServices<IHostedService>(), x => x is NotificationWorker);
    }

    [Fact]
    public void Enabled_RegistersNtfyServiceWorkerAndSink()
    {
        using var provider = BuildServices(
            ("Notifications:Enabled", "true"),
            ("Notifications:Errors:TopicUrl", "https://ntfy.local/errors"),
            ("Notifications:ErrorThrottle", "00:05:00")
        );

        Assert.IsType<NtfyNotificationService>(provider.GetRequiredService<INotificationService>());
        Assert.Contains(provider.GetServices<ILoggerProvider>(), x => x is ErrorNotificationLoggerProvider);
        Assert.Contains(provider.GetServices<IHostedService>(), x => x is NotificationWorker);

        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationOptions>>().Value;
        Assert.Equal("https://ntfy.local/errors", options.Errors!.TopicUrl);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ErrorThrottle);
    }
}
