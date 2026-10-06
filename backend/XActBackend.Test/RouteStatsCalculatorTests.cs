using AwesomeAssertions;
using NodaTime;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;

namespace XActBackend.Test;

public sealed class RouteStatsCalculatorTests
{
    private static readonly Instant Start = Instant.FromUtc(2026, 1, 1, 12, 0);
    private static readonly Instant End = Start + Duration.FromHours(1);

    // 0.0001 degrees of latitude are about 11.1 m
    private const double MetersPerStep = 11.12;

    private static TrackPoint Point(int seconds, double latitudeOffset, double accuracy = 5, TransportMode mode = TransportMode.Foot) =>
        new(Start + Duration.FromSeconds(seconds), 48.3 + latitudeOffset, 14.3, accuracy, mode, false);

    /// <summary>a straight walk north, one ping every interval seconds</summary>
    private static List<TrackPoint> Walk(int count, int intervalSeconds = 5, double stepDegrees = 0.0001, int startSecond = 0) =>
        Enumerable.Range(0, count).Select(i => Point(startSecond + i * intervalSeconds, i * stepDegrees)).ToList();

    [Fact]
    public void Calculate_ReturnsZeroStats_ForEmptyRoute()
    {
        RouteStats stats = RouteStatsCalculator.Calculate([], Start, End);

        stats.DistanceMeters.Should().Be(0);
        stats.TopSpeedMps.Should().Be(0);
        stats.AcceptedPoints.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_ReturnsZeroDistance_ForSinglePoint() =>
        RouteStatsCalculator.Calculate([Point(0, 0)], Start, End).DistanceMeters.Should().Be(0);

    [Fact]
    public void Calculate_SumsDistance_ForStraightWalk()
    {
        RouteStats stats = RouteStatsCalculator.Calculate(Walk(11), Start, End);

        stats.DistanceMeters.Should().BeApproximately(10 * MetersPerStep, 1);
        stats.MovingSeconds.Should().Be(50);
    }

    [Fact]
    public void Calculate_IgnoresJitter_WhenStandingStill()
    {
        // bounces around by about 3 m without going anywhere
        List<TrackPoint> points = Enumerable.Range(0, 20).Select(i => Point(i * 5, i % 2 == 0 ? 0 : 0.00003)).ToList();

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.DistanceMeters.Should().Be(0);
        stats.MovingSeconds.Should().Be(0);
    }

    [Fact]
    public void Calculate_DropsTeleportSpike()
    {
        List<TrackPoint> points = Walk(5);
        // one fix 5 km away for a single ping
        points.Insert(3, Point(12, 0.045));

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.DistanceMeters.Should().BeApproximately(4 * MetersPerStep, 1);
    }

    [Fact]
    public void Calculate_ReanchorsAfterConsecutiveRejections()
    {
        // the first fix is far off, every later fix is consistent with each other
        List<TrackPoint> points = [Point(0, 0.045), .. Walk(6, startSecond: 5)];

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.AcceptedPoints.Should().Contain(p => p.Timestamp == Start + Duration.FromSeconds(15));
        stats.DistanceMeters.Should().BeLessThan(10 * MetersPerStep);
    }

    [Fact]
    public void Calculate_DropsLowAccuracyFixes()
    {
        List<TrackPoint> points = Walk(5);
        points.Add(Point(25, 0.002, accuracy: 300));

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.DistanceMeters.Should().BeApproximately(4 * MetersPerStep, 1);
    }

    [Fact]
    public void Calculate_KeepsRoughFixes_WhenNoPreciseFixesExist()
    {
        // with such rough fixes only moves above the jitter cap count, so the 11 m steps add up to 33 m moves
        List<TrackPoint> points = Walk(7).Select(p => p with { AccuracyMeters = 900 }).ToList();

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.AcceptedPoints.Should().NotBeEmpty();
        stats.DistanceMeters.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Calculate_TopSpeed_IgnoresSingleShortSpike()
    {
        // jogging 2.2 m/s with one fix 30 m off, which on its own reads as an 8 m/s sprint for 5 s
        List<TrackPoint> spiked = Walk(30)
            .Select(p => p.Timestamp == Start + Duration.FromSeconds(75) ? p with { Latitude = p.Latitude + 0.00027 } : p)
            .ToList();

        RouteStats stats = RouteStatsCalculator.Calculate(spiked, Start, End);

        stats.TopSpeedMps.Should().BeLessThan(5);
    }

    [Fact]
    public void Calculate_TopSpeed_MatchesSteadyPace()
    {
        RouteStats stats = RouteStatsCalculator.Calculate(Walk(30, intervalSeconds: 5), Start, End);

        stats.TopSpeedMps.Should().BeApproximately(MetersPerStep / 5, 0.1);
    }

    [Fact]
    public void Calculate_SplitsTimeByTransportMode()
    {
        List<TrackPoint> points =
        [
            Point(0, 0),
            Point(10, 0.0001),
            Point(20, 0.0005, mode: TransportMode.Bus),
            Point(30, 0.0010, mode: TransportMode.Bus),
            Point(40, 0.0015, mode: TransportMode.Bus),
        ];

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.SecondsByMode[TransportMode.Foot].Should().Be(20);
        stats.SecondsByMode[TransportMode.Bus].Should().Be(20);
    }

    [Fact]
    public void Calculate_ExcludesLongGaps_FromMovingTimeAndTopSpeed()
    {
        // ten minutes offline, then the route continues 600 m further
        List<TrackPoint> points = [.. Walk(5), .. Walk(5, startSecond: 620).Select(p => p with { Latitude = p.Latitude + 0.0054 })];

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.MovingSeconds.Should().Be(40);
        stats.SecondsByMode[TransportMode.Foot].Should().Be(40);
        stats.TopSpeedMps.Should().BeLessThan(3);
        stats.DistanceMeters.Should().BeGreaterThan(600);
    }

    [Fact]
    public void Calculate_ClipsPointsOutsideMatchWindow()
    {
        List<TrackPoint> points = [Point(-60, 0.01), .. Walk(5)];

        RouteStats stats = RouteStatsCalculator.Calculate(points, Start, End);

        stats.DistanceMeters.Should().BeApproximately(4 * MetersPerStep, 1);
    }

    [Fact]
    public void PositionAt_InterpolatesBetweenPoints()
    {
        (double Latitude, double Longitude)? position =
            RouteStatsCalculator.PositionAt([Point(0, 0), Point(10, 0.001)], Start + Duration.FromSeconds(5), Duration.FromMinutes(2));

        position!.Value.Latitude.Should().BeApproximately(48.3005, 1e-9);
    }

    [Fact]
    public void PositionAt_ReturnsNull_AcrossGap() =>
        RouteStatsCalculator.PositionAt([Point(0, 0), Point(600, 0.001)], Start + Duration.FromSeconds(300), Duration.FromMinutes(2))
            .Should().BeNull();

    [Fact]
    public void PositionAt_ReturnsNull_BeforeTrackStarts() =>
        RouteStatsCalculator.PositionAt([Point(60, 0)], Start, Duration.FromMinutes(2)).Should().BeNull();
}
