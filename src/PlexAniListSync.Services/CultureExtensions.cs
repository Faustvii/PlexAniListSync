using System.Globalization;

namespace PlexAniListSync.Services;

public static class CultureExtensions
{

    public static (int result, bool parseResult) ToIntInvariantCulture(this string me)
    {
        var parseResult = int.TryParse(me, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
        return (result, parseResult);
    }

    public static string ToStringInvariantCulture(this int me)
    {
        return me.ToString(CultureInfo.InvariantCulture);
    }
}
