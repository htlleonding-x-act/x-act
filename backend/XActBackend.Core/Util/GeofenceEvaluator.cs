namespace XActBackend.Core.Util;

public static class GeofenceEvaluator
{
    /// <summary>
    ///     ray casting test over the polygon in sequence order. with fewer than three points there is no fence,
    ///     so every point counts as inside
    /// </summary>
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
