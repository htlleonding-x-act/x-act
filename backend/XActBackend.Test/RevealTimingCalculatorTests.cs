using AwesomeAssertions;
using NodaTime;
using XActBackend.Core.Util;

namespace XActBackend.Test;

public sealed class RevealTimingCalculatorTests
{
    [Fact]
    public void TryGetRevealWindow_ReturnsCurrentWindowAtStartTime()
    {
        var startTime = Instant.FromUtc(2026, 4, 14, 12, 0);
        var currentTime = startTime;

        var result = RevealTimingCalculator.TryGetRevealWindow(
            startTime,
            currentTime,
            5,
            out var intervalStart,
            out var intervalEnd,
            out var intervalSeconds,
            out var secondsRemaining);

        result.Should().BeTrue();
        intervalStart.Should().Be(startTime);
        intervalEnd.Should().Be(startTime.Plus(Duration.FromMinutes(5)));
        intervalSeconds.Should().Be(300);
        secondsRemaining.Should().Be(300);
    }

    [Fact]
    public void TryGetRevealWindow_ReturnsExpectedRemainingSecondsInMiddleOfInterval()
    {
        var startTime = Instant.FromUtc(2026, 4, 14, 12, 0);
        var currentTime = startTime.Plus(Duration.FromMinutes(7)).Plus(Duration.FromSeconds(15));

        var result = RevealTimingCalculator.TryGetRevealWindow(
            startTime,
            currentTime,
            5,
            out var intervalStart,
            out var intervalEnd,
            out _,
            out var secondsRemaining);

        result.Should().BeTrue();
        intervalStart.Should().Be(startTime.Plus(Duration.FromMinutes(5)));
        intervalEnd.Should().Be(startTime.Plus(Duration.FromMinutes(10)));
        secondsRemaining.Should().Be(165);
    }

    [Fact]
    public void TryGetRevealWindow_ReturnsFalse_WhenCurrentTimeIsBeforeStart()
    {
        var startTime = Instant.FromUtc(2026, 4, 14, 12, 0);
        var currentTime = startTime.Minus(Duration.FromSeconds(1));

        var result = RevealTimingCalculator.TryGetRevealWindow(
            startTime,
            currentTime,
            5,
            out var intervalStart,
            out var intervalEnd,
            out var intervalSeconds,
            out var secondsRemaining);

        result.Should().BeFalse();
        intervalStart.Should().Be(default);
        intervalEnd.Should().Be(default);
        intervalSeconds.Should().Be(0);
        secondsRemaining.Should().Be(0);
    }

    [Fact]
    public void TryGetRevealWindow_ReturnsFalse_WhenIntervalIsInvalid()
    {
        var startTime = Instant.FromUtc(2026, 4, 14, 12, 0);

        var result = RevealTimingCalculator.TryGetRevealWindow(
            startTime,
            startTime,
            0,
            out _,
            out _,
            out _,
            out _);

        result.Should().BeFalse();
    }
}

