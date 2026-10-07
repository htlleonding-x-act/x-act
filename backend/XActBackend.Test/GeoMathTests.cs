using AwesomeAssertions;
using XActBackend.Core.Util;

namespace XActBackend.Test;

public sealed class GeoMathTests
{
    [Fact]
    public void HaversineMeters_ReturnsZero_ForSamePoint() =>
        GeoMath.HaversineMeters(48.3, 14.3, 48.3, 14.3).Should().Be(0);

    [Fact]
    public void HaversineMeters_ReturnsAbout111Km_ForOneDegreeOfLatitude() =>
        GeoMath.HaversineMeters(48, 14, 49, 14).Should().BeApproximately(111_195, 50);

    [Fact]
    public void HaversineMeters_IsSymmetric() =>
        GeoMath.HaversineMeters(48.30, 14.28, 48.31, 14.29)
            .Should().BeApproximately(GeoMath.HaversineMeters(48.31, 14.29, 48.30, 14.28), 1e-9);
}
