using AwesomeAssertions;
using NodaTime;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;

namespace XActBackend.Test;

public sealed class RouteDownsamplerTests
{
    private static readonly Instant Start = Instant.FromUtc(2026, 1, 1, 12, 0);

    private static TrackPoint Point(int seconds, double latitudeOffset, bool revealed = false, TransportMode mode = TransportMode.Foot) =>
        new(Start + Duration.FromSeconds(seconds), 48.3 + latitudeOffset, 14.3, 5, mode, revealed);

    // pings 2 s and about 1 m apart, so most of them fall below the spacing
    private static List<TrackPoint> Dense(int count) =>
        Enumerable.Range(0, count).Select(i => Point(i * 2, i * 0.00001)).ToList();

    [Fact]
    public void Downsample_KeepsFirstLastAndRevealPoints()
    {
        List<TrackPoint> accepted = Dense(10);
        TrackPoint reveal = Point(7, 0.00004, revealed: true);

        IReadOnlyList<TrackPoint> result = RouteDownsampler.Downsample([.. accepted, reveal], accepted);

        result[0].Should().Be(accepted[0]);
        result[^1].Should().Be(accepted[^1]);
        result.Should().Contain(reveal);
    }

    [Fact]
    public void Downsample_KeepsTransportModeChanges()
    {
        List<TrackPoint> accepted = Dense(10);
        accepted[5] = accepted[5] with { Mode = TransportMode.Bus };

        IReadOnlyList<TrackPoint> result = RouteDownsampler.Downsample(accepted, accepted);

        result.Should().Contain(accepted[5]);
    }

    [Fact]
    public void Downsample_DropsPointsCloserThanMinimumSpacing() =>
        RouteDownsampler.Downsample(Dense(10), Dense(10)).Should().HaveCount(2);

    [Fact]
    public void Downsample_CapsPointCount()
    {
        List<TrackPoint> accepted = Enumerable.Range(0, 2000).Select(i => Point(i * 5, i * 0.0002)).ToList();

        IReadOnlyList<TrackPoint> result = RouteDownsampler.Downsample(accepted, accepted, maxPoints: 500);

        result.Count.Should().BeLessThanOrEqualTo(500);
        result[^1].Should().Be(accepted[^1]);
    }

    [Fact]
    public void Downsample_KeepsOnlyMandatoryPoints_WhenTheyFillTheBudget()
    {
        // first, last and two reveals are mandatory and use up all four places
        List<TrackPoint> accepted = Enumerable.Range(0, 20)
            .Select(i => Point(i * 5, i * 0.0002, revealed: i is 5 or 10))
            .ToList();

        IReadOnlyList<TrackPoint> result = RouteDownsampler.Downsample(accepted, accepted, maxPoints: 4);

        result.Should().Equal(accepted[0], accepted[5], accepted[10], accepted[19]);
    }
}
