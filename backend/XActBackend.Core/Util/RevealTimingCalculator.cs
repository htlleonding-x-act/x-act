using NodaTime;

namespace XActBackend.Core.Util;

public static class RevealTimingCalculator
{
    public static bool TryGetRevealWindow(
        Instant sessionStart,
        Instant currentTime,
        int revealIntervalMinutes,
        out Instant intervalStart,
        out Instant intervalEnd,
        out int revealIntervalSeconds,
        out int secondsRemaining)
    {
        intervalStart = default;
        intervalEnd = default;
        revealIntervalSeconds = 0;
        secondsRemaining = 0;

        if (revealIntervalMinutes <= 0 || revealIntervalMinutes > int.MaxValue / 60)
        {
            return false;
        }

        long elapsedSeconds = (long)currentTime.Minus(sessionStart).TotalSeconds;
        if (elapsedSeconds < 0)
        {
            return false;
        }

        revealIntervalSeconds = revealIntervalMinutes * 60;

        long intervalIndex = elapsedSeconds / revealIntervalSeconds;
        long intervalOffsetSeconds = intervalIndex * revealIntervalSeconds;

        intervalStart = sessionStart + Duration.FromSeconds(intervalOffsetSeconds);
        intervalEnd = intervalStart + Duration.FromSeconds(revealIntervalSeconds);
        secondsRemaining = (int)(intervalEnd.Minus(currentTime).TotalSeconds);

        return true;
    }
}


