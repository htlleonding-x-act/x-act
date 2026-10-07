namespace XActBackend.Core.Util;

public static class GeoMath
{
    // mean earth radius
    private const double EarthRadiusMeters = 6_371_008.8;

    public static double HaversineMeters(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double deltaLatitude = ToRadians(latitude2 - latitude1);
        double deltaLongitude = ToRadians(longitude2 - longitude1);

        double a = Math.Pow(Math.Sin(deltaLatitude / 2), 2)
                   + Math.Cos(ToRadians(latitude1)) * Math.Cos(ToRadians(latitude2)) * Math.Pow(Math.Sin(deltaLongitude / 2), 2);

        return 2 * EarthRadiusMeters * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    public static double HaversineMeters(TrackPoint from, TrackPoint to) =>
        HaversineMeters(from.Latitude, from.Longitude, to.Latitude, to.Longitude);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
