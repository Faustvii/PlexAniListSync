namespace PlexAniListSync.Models.Mappings;

public record EpisodeRuleMapping
{
    public EpisodeRule From { get; init; }
    public EpisodeRule To { get; init; }

    public EpisodeRuleMapping(EpisodeRule from, EpisodeRule to)
    {
        From = from;
        To = to;
    }
}
