using PlexAniListSync.AniListNet.Helpers;

namespace PlexAniListSync.AniListNet.Objects;

public class MediaEntry
{
    /// <summary>
    /// The ID of the list entry.
    /// </summary>
    [GqlSelection("id")]
    public int Id { get; init; }

    /// <summary>
    /// The watching/reading status.
    /// </summary>
    [GqlSelection("status")]
    public MediaEntryStatus Status { get; init; }

    /// <summary>
    /// The score of the entry.
    /// </summary>
    [GqlSelection("score")]
    public float Score { get; init; }

    /// <summary>
    /// The amount of episodes/chapters consumed by the user.
    /// </summary>
    [GqlSelection("progress")]
    public int Progress { get; init; }

    /// <summary>
    /// The amount of volumes read by the user.
    /// </summary>
    [GqlSelection("progressVolumes")]
    public int? VolumeProgress { get; init; }

    /// <summary>
    /// When the entry was started by the user.
    /// </summary>
    [GqlSelection("startedAt")]
    public Date StartDate { get; init; } = null!;

    /// <summary>
    /// When the entry was completed by the user.
    /// </summary>
    [GqlSelection("completedAt")]
    public Date CompleteDate { get; init; } = null!;

    [GqlSelection("media")]
    public Media Media { get; init; } = null!;

    [GqlSelection("notes")]
    public string? Notes { get; init; }

    /* below are properties that are not part of the API */

    /// <summary>
    /// The max possible progress of the anime or manga.
    /// </summary>
    public int? MaxProgress => Media.Episodes ?? Media.Chapters;

    /// <summary>
    /// The max possible volume progress of the manga.
    /// </summary>
    public int? MaxVolumeProgress => Media.Volumes;
}
