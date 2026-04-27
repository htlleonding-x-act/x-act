using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="GeofencePoint"/> entities.
/// </summary>
public interface IGeofencePointRepository
{
    /// <summary>
    ///     Add a new geofence point.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <param name="sequenceOrder">Order of the point in the geofence polygon</param>
    /// <returns>The created geofence point entity</returns>
    public GeofencePoint AddGeofencePoint(int sessionId, double latitude, double longitude, int sequenceOrder);

    /// <summary>
    ///     Get all geofence points for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All points for the session</returns>
    public ValueTask<IReadOnlyCollection<GeofencePoint>> GetPointsBySessionIdAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Get a geofence point by session id and point id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="pointId">The id of the geofence point</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The geofence point, if found</returns>
    public ValueTask<GeofencePoint?> GetPointBySessionAndIdAsync(int sessionId, int pointId, bool tracking);

    /// <summary>
    ///     Count the geofence points for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <returns>The number of geofence points for the session</returns>
    public ValueTask<int> CountPointsBySessionIdAsync(int sessionId);

    /// <summary>
    ///     Remove a geofence point from the repository.
    /// </summary>
    /// <param name="point">The geofence point to remove</param>
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

    public async ValueTask<GeofencePoint?> GetPointBySessionAndIdAsync(int sessionId, int pointId, bool tracking)
    {
        IQueryable<GeofencePoint> source = tracking ? Points : PointsNoTracking;

        return await source.FirstOrDefaultAsync(p => p.SessionId == sessionId && p.Id == pointId);
    }

    public async ValueTask<int> CountPointsBySessionIdAsync(int sessionId)
    {
        return await PointsNoTracking.CountAsync(p => p.SessionId == sessionId);
    }

    public void RemoveGeofencePoint(GeofencePoint point)
    {
        pointSet.Remove(point);
    }
}
