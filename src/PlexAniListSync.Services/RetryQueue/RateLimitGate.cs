namespace PlexAniListSync.Services.RetryQueue;

public sealed class RateLimitGate : IRateLimitGate
{
    private long _pausedUntilTicks;

    public DateTimeOffset? PausedUntil
    {
        get
        {
            var ticks = Interlocked.Read(ref _pausedUntilTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    public bool IsPaused(DateTimeOffset now)
    {
        var ticks = Interlocked.Read(ref _pausedUntilTicks);
        return ticks != 0 && now.UtcTicks < ticks;
    }

    public void Pause(DateTimeOffset until)
    {
        var untilTicks = until.UtcTicks;
        long current;
        do
        {
            current = Interlocked.Read(ref _pausedUntilTicks);
            if (untilTicks <= current)
                return;
        } while (Interlocked.CompareExchange(ref _pausedUntilTicks, untilTicks, current) != current);
    }
}
