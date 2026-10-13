using System;
using Microsoft.Extensions.Logging;
using Moq;
using PlexAniListSync.Services.Notifications;
using Xunit;

namespace PlexAniListSync.UnitTests.Notifications;

public class ErrorNotificationLoggerProviderTests
{
    private readonly Mock<INotificationService> _notifications = new();

    private ILogger BuildLogger(string category = "PlexAniListSync.Services.AniList.AniListService")
    {
        using var provider = new ErrorNotificationLoggerProvider(_notifications.Object);
        return provider.CreateLogger(category);
    }

    [Fact]
    public void Error_WithKnownEventId_ForwardsFriendlyMessage()
    {
        var logger = BuildLogger();

        logger.LogError(new EventId(3), "We were unable to authenticate 123 4 was not updated");

        _notifications.Verify(
            x => x.NotifyGlobalError(
                "PlexAniListSync.Services.AniList.AniListService:3",
                It.Is<string>(m => m.StartsWith("Failed to update AniList progress", StringComparison.Ordinal))
            ),
            Times.Once
        );
    }

    [Fact]
    public void Error_WithUnknownEventId_ForwardsGenericMessage()
    {
        var logger = BuildLogger("PlexAniListSync.Services.SomethingElse");

        logger.LogError(new EventId(99), "boom");

        _notifications.Verify(
            x => x.NotifyGlobalError(
                "PlexAniListSync.Services.SomethingElse:99",
                It.Is<string>(m => m.StartsWith("Unexpected error", StringComparison.Ordinal) && m.Contains("boom"))
            ),
            Times.Once
        );
    }

    [Theory]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    public void BelowError_IsIgnored(LogLevel level)
    {
        var logger = BuildLogger();

        logger.Log(level, new EventId(3), "not an error");

        _notifications.Verify(x => x.NotifyGlobalError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void FrameworkErrors_AreIgnored()
    {
        var logger = BuildLogger("Microsoft.AspNetCore.Server.Kestrel");

        logger.LogError(new EventId(1), "framework noise");

        _notifications.Verify(x => x.NotifyGlobalError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void NotificationMachineryErrors_AreIgnored()
    {
        // never notify about failing to notify
        var logger = BuildLogger("PlexAniListSync.Services.Notifications.NotificationWorker");

        logger.LogError(new EventId(24), "delivery failed");

        _notifications.Verify(x => x.NotifyGlobalError(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
