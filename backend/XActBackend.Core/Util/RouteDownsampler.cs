namespace XActBackend.Core.Util;

/// <summary>shrinks a route for the replay without losing reveals, transport changes or its shape</summary>
public static class RouteDownsampler
{
    public const int DefaultMaxPoints = 500;
    private const double MinSpacingMeters = 10;
    private const double MaxSpacingSeconds = 30;

    /// <param name="raw">every ping of the member, reveals are taken from here even when gps filtering dropped them</param>
    /// <param name="accepted">the pings the stats calculator kept</param>
    public static IReadOnlyList<TrackPoint> Downsample(IReadOnlyList<TrackPoint> raw, IReadOnlyList<TrackPoint> accepted, int maxPoints = DefaultMaxPoints)
    {
        List<TrackPoint> series = accepted
            .Concat(raw.Where(p => p.IsRevealed))
            .DistinctBy(p => p.Timestamp)
            .OrderBy(p => p.Timestamp)
            .ToList();

        if (series.Count <= 2)
        {
            return series;
        }

        List<(TrackPoint Point, bool Mandatory)> kept = [(series[0], true)];
        for (int i = 1; i < series.Count; i++)
        {
            TrackPoint point = series[i];
            TrackPoint lastKept = kept[^1].Point;
            bool mandatory = i == series.Count - 1 || point.IsRevealed || point.Mode != series[i - 1].Mode;
            bool spaced = GeoMath.HaversineMeters(lastKept, point) >= MinSpacingMeters
                          || (point.Timestamp - lastKept.Timestamp).TotalSeconds >= MaxSpacingSeconds;

            if (mandatory || spaced)
            {
                kept.Add((point, mandatory));
            }
        }

        return kept.Count <= maxPoints ? kept.Select(k => k.Point).ToList() : Thin(kept, maxPoints);
    }

    private static List<TrackPoint> Thin(List<(TrackPoint Point, bool Mandatory)> kept, int maxPoints)
    {
        int mandatoryCount = kept.Count(k => k.Mandatory);
        int optionalCount = kept.Count - mandatoryCount;
        int optionalBudget = Math.Max(0, maxPoints - mandatoryCount);
        int stride = optionalBudget == 0 ? int.MaxValue : (int)Math.Ceiling(optionalCount / (double)optionalBudget);

        List<TrackPoint> thinned = [];
        int optionalIndex = 0;
        foreach ((TrackPoint point, bool mandatory) in kept)
        {
            if (mandatory)
            {
                thinned.Add(point);
            }
            else
            {
                if (optionalIndex % stride == 0)
                {
                    thinned.Add(point);
                }

                optionalIndex++;
            }
        }

        return thinned;
    }
}
