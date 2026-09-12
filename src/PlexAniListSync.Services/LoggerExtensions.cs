using PlexAniListSync.AniListNet.Objects;
using Microsoft.Extensions.Logging;

namespace PlexAniListSync.Services;

#pragma warning disable MA0003
internal static class LoggerExtensions
{
    private static readonly Action<ILogger, string, Exception?> LogUnexpectedAmoutOfShowsAction;
    private static readonly Action<ILogger, string, Exception?> LogNoShowFoundAction;
    private static readonly Action<ILogger, int, int, Exception?> LogUnableToAuthenticateAnilistAction;
    private static readonly Action<ILogger, bool, int, int?, int?, string, Exception?> LogAnilistUpdateAction;
    private static readonly Action<ILogger, int, Exception?> LogAnilistRateLimitAction;
    private static readonly Action<ILogger, int, int, Exception?> LogAnilistUpdateSkippedAction;
    private static readonly Action<ILogger, string, Exception?> LogHostedServiceStartingAction;
    private static readonly Action<ILogger, string, Exception?> LogHostedServiceStoppingAction;
    private static readonly Action<ILogger, string, string, Exception?> LogFileDownloadErrorAction;
    private static readonly Action<ILogger, string, string, int, int, string, Exception?> LogWebhookUserWatchedAction;
    private static readonly Action<ILogger, string, int, Exception?> LogUnableToGetAnilistIdErrorAction;
    private static readonly Action<ILogger, string, int, Exception?> LogUnableToGetAnilistIdFromMappingsAction;
    private static readonly Action<ILogger, string, Exception?> LogUnableToFindTokenFromPlexUserAction;
    private static readonly Action<ILogger, string, Exception?> LogUnexpectedHostedServiceErrorAction;
    private static readonly Action<ILogger, string, string, Exception?> LogOnlyOneShowMatchedExactTitleAction;
    private static readonly Action<ILogger, string, int, Exception?> LogMutationQueuedRateLimitedAction;
    private static readonly Action<ILogger, string, int, Exception?> LogRetryQueueDisabledDroppingMutationAction;
    private static readonly Action<ILogger, string, int, int, Exception?> LogRetryQueueEntryExpiredAction;
    private static readonly Action<ILogger, string, int, Exception?> LogRetryQueueEntryUnresolvableAction;
    private static readonly Action<ILogger, int, int, int, Exception?> LogRetryQueueEntryDrainedAction;
    private static readonly Action<ILogger, int, Exception?> LogDrainRateLimitedOnUpdateAction;
    private static readonly Action<ILogger, int, Exception?> LogDrainRateLimitedOnResolveAction;

#pragma warning disable MA0051 // I'm okay with this being long
    static LoggerExtensions()
#pragma warning restore MA0051
    {
        LogUnexpectedAmoutOfShowsAction = LoggerMessage.Define<string>(
            logLevel: LogLevel.Warning,
            eventId: 1,
            formatString: "We got {Scenario} results trying to search by title"
        );

        LogNoShowFoundAction = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            eventId: 2,
            formatString: "We got no matches with '{Query}' - Let's try a bit less specific"
        );

        LogUnableToAuthenticateAnilistAction = LoggerMessage.Define<int, int>(
            logLevel: LogLevel.Error,
            eventId: 3,
            formatString: "We were unable to authenticate {AnilistId} {Episode} was not updated"
        );

        LogAnilistUpdateAction = LoggerMessage.Define<bool, int, int?, int?, string>(
            logLevel: LogLevel.Information,
            eventId: 4,
            formatString: "TestMode: '{TestMode}' Updating Anilist {AniListId} {Episode} out of {MaxEpisodes} - Status '{Status}'"
        );

        LogAnilistRateLimitAction = LoggerMessage.Define<int>(
            logLevel: LogLevel.Debug,
            eventId: 5,
            formatString: "Ratelimit left {RateRemaining}"
        );

        LogAnilistUpdateSkippedAction = LoggerMessage.Define<int, int>(
            logLevel: LogLevel.Warning,
            eventId: 6,
            formatString: "Anilist has {MaxProgress} episodes watched and we just watched {Episode} episode (Are we rewatching?) - Skipping update"
        );

        LogHostedServiceStartingAction = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            eventId: 7,
            formatString: "{Service} is starting."
        );

        LogHostedServiceStoppingAction = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            eventId: 8,
            formatString: "{Service} is stopping."
        );

        LogFileDownloadErrorAction = LoggerMessage.Define<string, string>(
            logLevel: LogLevel.Error,
            eventId: 9,
            formatString: "Error downloading file from '{Url}' - Error {ErrorMessage}"
        );

        LogWebhookUserWatchedAction = LoggerMessage.Define<string, string, int, int, string>(
            logLevel: LogLevel.Information,
            eventId: 10,
            formatString: "{User} watched {ShowTitle} episode {Episode} from season {Season} - PlexGuid: {PlexGuid}"
        );

        LogUnableToGetAnilistIdErrorAction = LoggerMessage.Define<string, int>(
            logLevel: LogLevel.Error,
            eventId: 12,
            formatString: "We were unable to get AniListId from AniList for {ShowTitle} - S{Season} - Aborting"
        );

        LogUnableToGetAnilistIdFromMappingsAction = LoggerMessage.Define<string, int>(
            logLevel: LogLevel.Information,
            eventId: 13,
            formatString: "We were unable to get AniListId from {ShowTitle} - S{Season} from mapping files - Trying Anilist API search"
        );

        LogUnableToFindTokenFromPlexUserAction = LoggerMessage.Define<string>(
            logLevel: LogLevel.Warning,
            eventId: 14,
            formatString: "We were unable to find a Anilist token from plex username '{PlexUsername}'"
        );

        LogUnexpectedHostedServiceErrorAction = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            eventId: 15,
            formatString: "Unexpected error happened in {Service}"
        );

        LogOnlyOneShowMatchedExactTitleAction = LoggerMessage.Define<string, string>(
            logLevel: LogLevel.Information,
            eventId: 16,
            formatString: "We managed to find only one exact match from '{PossibleShows}' with '{Title}'"
        );

        LogMutationQueuedRateLimitedAction = LoggerMessage.Define<string, int>(
            logLevel: LogLevel.Warning,
            eventId: 17,
            formatString: "AniList is rate-limiting - queued '{ShowTitle}' episode {Episode} for a durable retry"
        );

        LogRetryQueueDisabledDroppingMutationAction = LoggerMessage.Define<string, int>(
            logLevel: LogLevel.Warning,
            eventId: 18,
            formatString: "Retry queue is disabled - dropping '{ShowTitle}' episode {Episode} after an AniList 429"
        );

        LogRetryQueueEntryExpiredAction = LoggerMessage.Define<string, int, int>(
            logLevel: LogLevel.Warning,
            eventId: 19,
            formatString: "Dropping stale retry-queue entry '{ShowTitle}' episode {Episode} after {Attempts} attempts"
        );

        LogRetryQueueEntryUnresolvableAction = LoggerMessage.Define<string, int>(
            logLevel: LogLevel.Warning,
            eventId: 20,
            formatString: "Dropping retry-queue entry '{ShowTitle}' S{Season} - no longer resolves to an AniList id"
        );

        LogRetryQueueEntryDrainedAction = LoggerMessage.Define<int, int, int>(
            logLevel: LogLevel.Information,
            eventId: 21,
            formatString: "Drained retry-queue entry - AniList {AniListId} episode {Episode} ({Coalesced} webhook(s) coalesced)"
        );

        LogDrainRateLimitedOnUpdateAction = LoggerMessage.Define<int>(
            logLevel: LogLevel.Warning,
            eventId: 22,
            formatString: "AniList rate-limited applying AniList {AniListId} during drain - pausing and rescheduling with backoff"
        );

        LogDrainRateLimitedOnResolveAction = LoggerMessage.Define<int>(
            logLevel: LogLevel.Warning,
            eventId: 23,
            formatString: "AniList rate-limited resolving episode {Episode} during drain - pausing and rescheduling with backoff"
        );
    }

    public static void LogUnexpectedAmoutOfShows(this ILogger logger, int showCount)
    {
        LogUnexpectedAmoutOfShowsAction(logger, showCount > 1 ? "multiple" : "no", null);
    }

    public static void LogNoShowFound(this ILogger logger, string query)
    {
        LogNoShowFoundAction(logger, query, null);
    }

    public static void LogUnableToAuthenticateAnilist(this ILogger logger, int anilistId, int episode)
    {
        LogUnableToAuthenticateAnilistAction(logger, anilistId, episode, null);
    }

    public static void LogAnilistUpdate(
        this ILogger logger,
        bool testMode,
        int anilistId,
        int? episode,
        int? maxEpisodes,
        MediaEntryStatus status
    )
    {
        LogAnilistUpdateAction(logger, testMode, anilistId, episode, maxEpisodes, status.ToString(), null);
    }

    public static void LogAnilistRatelimit(this ILogger logger, int rateRemaining)
    {
        LogAnilistRateLimitAction(logger, rateRemaining, null);
    }

    public static void LogAnilistUpdateSkipped(this ILogger logger, int maxProgress, int episode)
    {
        LogAnilistUpdateSkippedAction(logger, maxProgress, episode, null);
    }

    public static void LogHostedServiceStarting(this ILogger logger, string serviceName)
    {
        LogHostedServiceStartingAction(logger, serviceName, null);
    }

    public static void LogHostedServiceStopping(this ILogger logger, string serviceName)
    {
        LogHostedServiceStoppingAction(logger, serviceName, null);
    }

    public static void LogFileDownloadError(this ILogger logger, string url, string errorMessage, Exception? ex = null)
    {
        LogFileDownloadErrorAction(logger, url, errorMessage, ex);
    }

    public static void LogWebhookUserWatched(
        this ILogger logger,
        string user,
        string show,
        int episode,
        int season,
        string plexGuid
    )
    {
        LogWebhookUserWatchedAction(logger, user, show, episode, season, plexGuid, null);
    }

    public static void LogUnableToGetAnilistIdFromMappings(this ILogger logger, string show, int season)
    {
        LogUnableToGetAnilistIdFromMappingsAction(logger, show, season, null);
    }

    public static void LogUnableToGetAnilistIdError(this ILogger logger, string show, int season)
    {
        LogUnableToGetAnilistIdErrorAction(logger, show, season, null);
    }

    public static void LogUnableToFindTokenFromPlexUser(this ILogger logger, string username)
    {
        LogUnableToFindTokenFromPlexUserAction(logger, username, null);
    }

    public static void LogUnexpectedHostedServiceError(this ILogger logger, string hostedService, Exception ex)
    {
        LogUnexpectedHostedServiceErrorAction(logger, hostedService, ex);
    }

    public static void LogOnlyOneShowMatchedExactTitle(
        this ILogger logger,
        string title,
        IEnumerable<string> possibleShows
    )
    {
        LogOnlyOneShowMatchedExactTitleAction(logger, string.Join(", ", possibleShows), title, null);
    }

    public static void LogMutationQueuedRateLimited(this ILogger logger, string showTitle, int episode)
    {
        LogMutationQueuedRateLimitedAction(logger, showTitle, episode, null);
    }

    public static void LogRetryQueueDisabledDroppingMutation(this ILogger logger, string showTitle, int episode)
    {
        LogRetryQueueDisabledDroppingMutationAction(logger, showTitle, episode, null);
    }

    public static void LogRetryQueueEntryExpired(this ILogger logger, string showTitle, int episode, int attempts)
    {
        LogRetryQueueEntryExpiredAction(logger, showTitle, episode, attempts, null);
    }

    public static void LogRetryQueueEntryUnresolvable(this ILogger logger, string showTitle, int season)
    {
        LogRetryQueueEntryUnresolvableAction(logger, showTitle, season, null);
    }

    public static void LogRetryQueueEntryDrained(this ILogger logger, int aniListId, int episode, int coalesced)
    {
        LogRetryQueueEntryDrainedAction(logger, aniListId, episode, coalesced, null);
    }

    public static void LogDrainRateLimitedOnUpdate(this ILogger logger, int aniListId)
    {
        LogDrainRateLimitedOnUpdateAction(logger, aniListId, null);
    }

    public static void LogDrainRateLimitedOnResolve(this ILogger logger, int episode)
    {
        LogDrainRateLimitedOnResolveAction(logger, episode, null);
    }
}
#pragma warning restore MA0003
