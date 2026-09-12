using System.Reflection;
using System.Runtime.Serialization;

namespace PlexAniListSync.AniListNet.Helpers;

internal static class HelperUtilities
{
    public static string GetEnumMemberValue(Enum @enum)
    {
        var field = @enum.GetType().GetField(@enum.ToString());
        var attribute = field?.GetCustomAttribute<EnumMemberAttribute>();
        return attribute?.Value ?? @enum.ToString();
    }

    public static DateTime? ResolveRateReset(int? rateResetUnixSeconds, int? retryAfterSeconds)
    {
        if (rateResetUnixSeconds.HasValue)
            return DateTimeOffset.FromUnixTimeSeconds(rateResetUnixSeconds.Value).DateTime;
        if (retryAfterSeconds.HasValue)
            return DateTimeOffset.UtcNow.AddSeconds(retryAfterSeconds.Value).DateTime;
        return null;
    }
}
