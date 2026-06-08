namespace XActBackend.Core.Util;

/// <summary>
///     Geometry helper for deciding whether a coordinate lies inside a session's geofence polygon.
/// </summary>
public static class GeofenceEvaluator
{
    /// <summary>
    ///     Tests whether the point (<paramref name="latitude"/>, <paramref name="longitude"/>) lies
    ///     inside the polygon described by <paramref name="polygon"/> (in sequence order) using the
    ///     ray-casting algorithm. A polygon with fewer than three vertices is treated as "no fence",
    ///     so every point counts as inside and no out-of-bounds offense is raised.
    /// </summary>
    /// <param name="latitude">Latitude of the point to test, in decimal degrees</param>
    /// <param name="longitude">Longitude of the point to test, in decimal degrees</param>
    /// <param name="polygon">The polygon vertices (latitude, longitude) in sequence order</param>
    /// <returns><c>true</c> if the point is inside the polygon or no usable fence exists</returns>
    public static bool IsInsidePolygon(
        double latitude,
        double longitude,
        IReadOnlyList<(double Latitude, double Longitude)> polygon)
    {
        if (polygon.Count < 3)
        {
            return true;
        }

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            double latI = polygon[i].Latitude;
            double lonI = polygon[i].Longitude;
            double latJ = polygon[j].Latitude;
            double lonJ = polygon[j].Longitude;

            bool crossesLatitude = (latI > latitude) != (latJ > latitude);
            if (crossesLatitude)
            {
                double intersectionLongitude = (lonJ - lonI) * (latitude - latI) / (latJ - latI) + lonI;
                if (longitude < intersectionLongitude)
                {
                    inside = !inside;
                }
            }
        }

        return inside;
    }
}
