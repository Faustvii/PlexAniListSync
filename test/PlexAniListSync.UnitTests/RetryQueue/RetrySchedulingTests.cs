using System;
using PlexAniListSync.AniListNet;
using PlexAniListSync.Models.RetryQueue;
using PlexAniListSync.Services.RetryQueue;
using Xunit;

namespace PlexAniListSync.UnitTests.RetryQueue;

public class RetrySchedulingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static RetryQueueOptions Options() =>
        new()
        {
            BaseBackoffSeconds = 300,
            MaxBackoffSeconds = 7200,
            TtlHours = 24,
            DefaultPauseSeconds = 60,
        };

    [Theory]
    [InlineData(1, 300)]
    [InlineData(2, 600)]
    [InlineData(3, 1200)]
    [InlineData(10, 7200)]
    [InlineData(40, 7200)]
    public void NextAttempt_IsExponentialAndCapped(int attempts, int expectedSeconds)
    {
        var next = RetryScheduling.NextAttempt(attempts, Now, Options(), notBefore: null);

        Assert.Equal(Now.AddSeconds(expectedSeconds), next);
    }

    [Fact]
    public void NextAttempt_NeverEarlierThanRateReset()
    {
        var reset = Now.AddHours(1);

        var next = RetryScheduling.NextAttempt(1, Now, Options(), notBefore: reset);

        Assert.Equal(reset, next);
    }

    [Fact]
    public void NextAttempt_IgnoresRateResetThatIsSoonerThanBackoff()
    {
        var reset = Now.AddSeconds(10);

        var next = RetryScheduling.NextAttempt(1, Now, Options(), notBefore: reset);

        Assert.Equal(Now.AddSeconds(300), next);
    }

    [Fact]
    public void IsExpired_TrueOnlyPastTtl()
    {
        var options = Options();

        Assert.False(RetryScheduling.IsExpired(Now.AddHours(-23), Now, options));
        Assert.True(RetryScheduling.IsExpired(Now.AddHours(-25), Now, options));
    }

    [Fact]
    public void PauseUntil_UsesRateResetWhenInTheFuture()
    {
        var resetUnix = (int)Now.AddMinutes(30).ToUnixTimeSeconds();
        var exception = new AniListRateLimitException("req", "body", retryAfter: 5, rateResetUnixSeconds: resetUnix);

        var until = RetryScheduling.PauseUntil(exception, Now, Options());

        Assert.Equal(Now.AddMinutes(30).ToUnixTimeSeconds(), until.ToUnixTimeSeconds());
    }

    [Fact]
    public void PauseUntil_FallsBackToDefaultWhenNoHeaders()
    {
        var exception = new AniListRateLimitException("req", "body", retryAfter: null, rateResetUnixSeconds: null);

        var until = RetryScheduling.PauseUntil(exception, Now, Options());

        Assert.Equal(Now.AddSeconds(60), until);
    }
}
