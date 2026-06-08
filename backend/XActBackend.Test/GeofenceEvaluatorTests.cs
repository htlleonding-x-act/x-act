using AwesomeAssertions;
using XActBackend.Core.Util;

namespace XActBackend.Test;

public sealed class GeofenceEvaluatorTests
{
    // A simple square spanning lat/lon 0..10.
    private static readonly IReadOnlyList<(double Latitude, double Longitude)> Square =
    [
        (0, 0),
        (0, 10),
        (10, 10),
        (10, 0),
    ];

    [Fact]
    public void IsInsidePolygon_ReturnsTrue_ForPointInsideSquare() =>
        GeofenceEvaluator.IsInsidePolygon(5, 5, Square).Should().BeTrue();

    [Fact]
    public void IsInsidePolygon_ReturnsFalse_ForPointOutsideSquare() =>
        GeofenceEvaluator.IsInsidePolygon(5, 20, Square).Should().BeFalse();

    [Fact]
    public void IsInsidePolygon_ReturnsFalse_ForPointBelowSquare() =>
        GeofenceEvaluator.IsInsidePolygon(-1, 5, Square).Should().BeFalse();

    [Fact]
    public void IsInsidePolygon_TreatsDegeneratePolygonAsNoFence() =>
        GeofenceEvaluator.IsInsidePolygon(100, 100, [(0, 0), (0, 10)]).Should().BeTrue();
}
