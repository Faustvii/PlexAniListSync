using System;
using PlexAniListSync.Services.RetryQueue;
using Xunit;

namespace PlexAniListSync.UnitTests.RetryQueue;

public class RateLimitGateTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NotPaused_ByDefault()
    {
        var gate = new RateLimitGate();

        Assert.Null(gate.PausedUntil);
        Assert.False(gate.IsPaused(Now));
    }

    [Fact]
    public void IsPaused_TrueUntilResetInstant()
    {
        var gate = new RateLimitGate();
        var until = Now.AddMinutes(5);

        gate.Pause(until);

        Assert.True(gate.IsPaused(Now));
        Assert.False(gate.IsPaused(until));
        Assert.False(gate.IsPaused(until.AddSeconds(1)));
    }

    [Fact]
    public void Pause_KeepsTheFurthestOutReset()
    {
        var gate = new RateLimitGate();

        gate.Pause(Now.AddMinutes(10));
        gate.Pause(Now.AddMinutes(2));

        Assert.Equal(Now.AddMinutes(10), gate.PausedUntil);
    }
}
