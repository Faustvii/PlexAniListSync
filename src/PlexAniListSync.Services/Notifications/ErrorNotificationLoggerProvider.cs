using Microsoft.Extensions.Logging;

namespace PlexAniListSync.Services.Notifications;

internal sealed class ErrorNotificationLoggerProvider : ILoggerProvider
{
    private readonly INotificationService _notifications;

    public ErrorNotificationLoggerProvider(INotificationService notifications) => _notifications = notifications;

    public ILogger CreateLogger(string categoryName) => new ErrorNotificationLogger(categoryName, _notifications);

    public void Dispose() { }

    private sealed class ErrorNotificationLogger : ILogger
    {
        private static readonly Dictionary<int, string> FriendlyMessages = new()
        {
            { 3, "Failed to update AniList progress - authentication failed (token expired?)" },
            { 9, "Failed to download a mapping file" },
            { 12, "Unable to resolve an AniList id for watched media" },
            { 15, "Unexpected error in a background service" },
        };

        private readonly string _categoryName;
        private readonly INotificationService _notifications;

        public ErrorNotificationLogger(string categoryName, INotificationService notifications)
        {
            _categoryName = categoryName;
            _notifications = notifications;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            if (!IsEnabled(logLevel))
                return;

            var details = formatter(state, exception);
            var friendly = FriendlyMessages.GetValueOrDefault(eventId.Id, "Unexpected error");
            _notifications.NotifyGlobalError($"{_categoryName}:{eventId.Id}", $"{friendly}\n{details}");
        }

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Error
            && _categoryName.StartsWith("PlexAniListSync", StringComparison.Ordinal)
            && !_categoryName.StartsWith($"{nameof(PlexAniListSync)}.Services.Notifications", StringComparison.Ordinal);

        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose() { }
        }
    }
}
