using AniListNet.Objects;

namespace PlexAniListSync.Services.AniList;

/// <summary>
/// The parts of a <see cref="MediaEntry"/> we need to make an update decision,
/// in a plain shape that serializes cleanly to the L2 cache (SQLite/Redis).
/// </summary>
public class CachedMediaEntry
{
    public int Progress { get; set; }
    public int? MaxProgress { get; set; }
    public DateTime? StartDate { get; set; }

    public static CachedMediaEntry From(MediaEntry entry) =>
        new()
        {
            Progress = entry.Progress,
            MaxProgress = entry.MaxProgress,
            StartDate = entry.StartDate.ToDateTime(),
        };
}
