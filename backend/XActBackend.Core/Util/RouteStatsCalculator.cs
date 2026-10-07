using XActBackend.Persistence.Model;

namespace XActBackend.Core.Util;

public sealed record RouteStats(
    double DistanceMeters,
    double TopSpeedMps,
    double MovingSeconds,
    IReadOnlyDictionary<TransportMode, double> SecondsByMode,
    IReadOnlyList<TrackPoint> AcceptedPoints
);

/// <summary>
///     turns raw gps pings into distance, speed and time stats. phones report noisy positions, so standing still
///     must not add distance and a single wild fix must not set the top speed
/// </summary>
public static class RouteStatsCalculator
{
    public static class Limits
    {
        public const double MaxAccuracyMeters = 50;

        // a move shorter than the position uncertainty is treated as jitter
        public const double MinMoveMeters = 8;
        public const double MaxAccuracyJitterMeters = 25;

        // about 250 km/h, fast enough for any train the game area can have
        public const double MaxPlausibleSpeedMps = 70;

        // after this many implausible fixes in a row the anchor itself was the outlier
        public const int MaxRejectionsBeforeReanchor = 3;

        // pings arrive every few seconds, a longer silence means the app was in the background or offline
        public const double MaxGapSeconds = 120;

        public const double MovingSpeedMps = 0.5;
        public const double TopSpeedWindowSeconds = 20;
    }

    private enum StepKind
    {
        Jitter,
        Move,
        Teleport,
        Reanchor,
    }

    public static RouteStats Calculate(IReadOnlyList<TrackPoint> points, Instant start, Instant end)
    {
        List<TrackPoint> candidates = PrepareCandidates(points, start, end);
        Dictionary<TransportMode, double> secondsByMode = [];

        if (candidates.Count == 0)
        {
            return new RouteStats(0, 0, 0, secondsByMode, []);
        }

        List<TrackPoint> accepted = [candidates[0]];
        List<double> cumulativeDistance = [0];
        TrackPoint anchor = candidates[0];
        double distance = 0;
        double movingSeconds = 0;
        int rejections = 0;

        for (int i = 1; i < candidates.Count; i++)
        {
            TrackPoint point = candidates[i];
            AddTime(secondsByMode, candidates[i - 1], point);

            double stepMeters = GeoMath.HaversineMeters(anchor, point);
            double stepSeconds = (point.Timestamp - anchor.Timestamp).TotalSeconds;

            // jitter keeps the anchor, so after standing still for minutes its timestamp is old and a wild fix
            // spread over that whole time would look like a plausible walk. the player was last seen near the
            // anchor at the previous fix, so the speed check counts from there
            double secondsSincePrevious = (point.Timestamp - candidates[i - 1].Timestamp).TotalSeconds;

            switch (ClassifyStep(anchor, point, stepMeters, secondsSincePrevious, rejections))
            {
                case StepKind.Jitter:
                    // a fix close to the anchor confirms it, so earlier outliers no longer count towards a re-anchor
                    rejections = 0;
                    break;
                case StepKind.Teleport:
                    rejections++;
                    break;
                case StepKind.Reanchor:
                    anchor = point;
                    rejections = 0;
                    accepted.Add(point);
                    cumulativeDistance.Add(distance);
                    break;
                case StepKind.Move:
                    distance += stepMeters;
                    if (stepSeconds <= Limits.MaxGapSeconds && stepMeters / stepSeconds >= Limits.MovingSpeedMps)
                    {
                        movingSeconds += stepSeconds;
                    }

                    anchor = point;
                    rejections = 0;
                    accepted.Add(point);
                    cumulativeDistance.Add(distance);
                    break;
            }
        }

        double topSpeed = TopSpeed(accepted, cumulativeDistance);

        return new RouteStats(distance, topSpeed, movingSeconds, secondsByMode, accepted);
    }

    /// <summary>
    ///     the position at a moment, interpolated between the surrounding points of a sorted track. null before the
    ///     track starts, and when the closest earlier point is older than maxAge with no close later point
    /// </summary>
    public static (double Latitude, double Longitude)? PositionAt(IReadOnlyList<TrackPoint> track, Instant at, Duration maxAge)
    {
        int index = LastIndexAtOrBefore(track, at);
        if (index < 0)
        {
            return null;
        }

        TrackPoint before = track[index];
        if (index + 1 < track.Count)
        {
            TrackPoint after = track[index + 1];
            double gapSeconds = (after.Timestamp - before.Timestamp).TotalSeconds;
            if (gapSeconds <= Limits.MaxGapSeconds)
            {
                double fraction = gapSeconds > 0 ? (at - before.Timestamp).TotalSeconds / gapSeconds : 0;

                return (before.Latitude + (after.Latitude - before.Latitude) * fraction,
                        before.Longitude + (after.Longitude - before.Longitude) * fraction);
            }
        }

        return at - before.Timestamp <= maxAge ? (before.Latitude, before.Longitude) : null;
    }

    private static List<TrackPoint> PrepareCandidates(IReadOnlyList<TrackPoint> points, Instant start, Instant end)
    {
        List<TrackPoint> inMatch = points
            .Where(p => p.Timestamp >= start && p.Timestamp <= end)
            .OrderBy(p => p.Timestamp)
            .DistinctBy(p => p.Timestamp)
            .ToList();

        List<TrackPoint> precise = inMatch.Where(p => p.AccuracyMeters <= Limits.MaxAccuracyMeters).ToList();

        // desktop browsers locate by wifi with accuracies in the kilometers. dropping every fix would leave no
        // route at all, so a track without enough precise fixes keeps the rough ones
        return precise.Count >= 2 ? precise : inMatch;
    }

    private static StepKind ClassifyStep(TrackPoint anchor, TrackPoint point, double stepMeters, double secondsSincePrevious,
                                         int rejections)
    {
        if (stepMeters / secondsSincePrevious > Limits.MaxPlausibleSpeedMps)
        {
            return rejections + 1 >= Limits.MaxRejectionsBeforeReanchor ? StepKind.Reanchor : StepKind.Teleport;
        }

        double jitterMeters = Math.Max(Limits.MinMoveMeters,
                                       Math.Min(Limits.MaxAccuracyJitterMeters, (anchor.AccuracyMeters + point.AccuracyMeters) / 2));

        return stepMeters < jitterMeters ? StepKind.Jitter : StepKind.Move;
    }

    private static void AddTime(Dictionary<TransportMode, double> secondsByMode, TrackPoint previous, TrackPoint point)
    {
        double seconds = (point.Timestamp - previous.Timestamp).TotalSeconds;
        if (seconds <= Limits.MaxGapSeconds)
        {
            secondsByMode[previous.Mode] = secondsByMode.GetValueOrDefault(previous.Mode) + seconds;
        }
    }

    /// <summary>best average speed over any window of at least TopSpeedWindowSeconds that spans no gap</summary>
    private static double TopSpeed(List<TrackPoint> accepted, List<double> cumulativeDistance)
    {
        double topSpeed = 0;
        int windowStart = 0;
        int lastGapIndex = 0;

        for (int i = 1; i < accepted.Count; i++)
        {
            if ((accepted[i].Timestamp - accepted[i - 1].Timestamp).TotalSeconds > Limits.MaxGapSeconds)
            {
                lastGapIndex = i;
            }

            windowStart = Math.Max(windowStart, lastGapIndex);
            while (windowStart + 1 < i
                   && (accepted[i].Timestamp - accepted[windowStart + 1].Timestamp).TotalSeconds >= Limits.TopSpeedWindowSeconds)
            {
                windowStart++;
            }

            double windowSeconds = (accepted[i].Timestamp - accepted[windowStart].Timestamp).TotalSeconds;
            if (windowSeconds >= Limits.TopSpeedWindowSeconds)
            {
                double speed = (cumulativeDistance[i] - cumulativeDistance[windowStart]) / windowSeconds;
                topSpeed = Math.Max(topSpeed, speed);
            }
        }

        return topSpeed;
    }

    private static int LastIndexAtOrBefore(IReadOnlyList<TrackPoint> track, Instant at)
    {
        int low = 0;
        int high = track.Count - 1;
        int result = -1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            if (track[middle].Timestamp <= at)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return result;
    }
}
