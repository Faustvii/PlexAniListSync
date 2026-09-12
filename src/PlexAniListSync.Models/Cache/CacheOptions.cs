namespace PlexAniListSync.Models.Cache;

public class CacheOptions
{
    public const string Key = "Cache";

    /// <summary>
    /// Which persistent (L2) backend to place behind the in-memory (L1) cache.
    /// "Sqlite" (default), "Redis" or "Memory" (no persistence).
    /// </summary>
    public CacheBackend Backend { get; set; } = CacheBackend.Sqlite;

    /// <summary>
    /// Path to the SQLite cache file when <see cref="Backend"/> is Sqlite.
    /// Defaults next to the existing config so it lands on the persistent volume.
    /// </summary>
    public string SqlitePath { get; set; } = "config/cache.db";

    /// <summary>
    /// StackExchange.Redis connection string when <see cref="Backend"/> is Redis.
    /// </summary>
    public string RedisConnection { get; set; } = string.Empty;

    /// <summary>How long a valid (found) title -&gt; AniList id lookup is cached.</summary>
    public int LookupTtlHours { get; set; } = 24;

    /// <summary>How long a "not found" title lookup is cached (kept short so new mappings take effect).</summary>
    public int NotFoundTtlHours { get; set; } = 1;

    /// <summary>How long a per-user media entry (progress) read is cached.</summary>
    public int MediaEntryTtlHours { get; set; } = 24;
}

public enum CacheBackend
{
    Sqlite,
    Redis,
    Memory,
}
