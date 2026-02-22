using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IGeofencePointRepository
{
    public GeofencePoint AddGeofencePoint(int sessionId, double latitude, double longitude, int sequenceOrder);
    public ValueTask<IReadOnlyCollection<GeofencePoint>> GetPointsBySessionIdAsync(int sessionId, bool tracking);
    public void RemoveGeofencePoint(GeofencePoint point);
}

internal sealed class GeofencePointRepository(DbSet<GeofencePoint> pointSet) : IGeofencePointRepository
{
    private IQueryable<GeofencePoint> Points => pointSet;
    private IQueryable<GeofencePoint> PointsNoTracking => Points.AsNoTracking();

    public GeofencePoint AddGeofencePoint(int sessionId, double latitude, double longitude, int sequenceOrder)
    {
        var point = new GeofencePoint
        {
            SessionId = sessionId,
            Latitude = latitude,
            Longitude = longitude,
            SequenceOrder = sequenceOrder,
        };

        pointSet.Add(point);

        return point;
    }

    public async ValueTask<IReadOnlyCollection<GeofencePoint>> GetPointsBySessionIdAsync(int sessionId, bool tracking)
    {
        IQueryable<GeofencePoint> source = tracking ? Points : PointsNoTracking;

        List<GeofencePoint> points = await source
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.SequenceOrder)
            .ToListAsync();

        return points;
    }

    public void RemoveGeofencePoint(GeofencePoint point)
    {
        pointSet.Remove(point);
    }
}
