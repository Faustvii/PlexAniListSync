using PlexAniListSync.AniListNet.Helpers;

namespace PlexAniListSync.AniListNet.Objects;

public class Date
{
    [GqlSelection("year")]
    public int? Year { get; init; }

    [GqlSelection("month")]
    public int? Month { get; init; }

    [GqlSelection("day")]
    public int? Day { get; init; }

    public DateTime? ToDateTime()
    {
        if (Year.HasValue && Month.HasValue && Day.HasValue)
            return new DateTime(Year.Value, Month.Value, Day.Value);
        return null;
    }
}
