using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Provides methods to manage geofence points for sessions.
/// </summary>
public interface IGeofencePointService
{
    /// <summary>
    ///     Get all geofence points for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All geofence points ordered by sequence</returns>
    public ValueTask<IReadOnlyCollection<GeofencePoint>> GetAllPointsBySessionIdAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Get a geofence point by id for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="pointId">The id of the geofence point</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The geofence point, if found</returns>
    public ValueTask<OneOf<GeofencePoint, NotFound>> GetGeofencePointByIdAsync(int sessionId, int pointId, bool tracking);

    /// <summary>
    ///     Add a new geofence point.
    /// </summary>
    /// <param name="newGeofencePoint">The geofence point data to create</param>
    /// <returns>The created geofence point, not found if the session does not exist, or a domain error</returns>
    public ValueTask<OneOf<GeofencePoint, NotFound, DomainError>> AddGeofencePointAsync(GeofencePointData newGeofencePoint);

    /// <summary>
    ///     Update an existing geofence point.
    /// </summary>
    /// <param name="pointId">The id of the geofence point to update</param>
    /// <param name="geofencePointData">The new geofence point data</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the update was successful</returns>
    public ValueTask<OneOf<Success, NotFound>> UpdateGeofencePointAsync(int pointId, GeofencePointData geofencePointData, bool tracking);

    /// <summary>
    ///     Delete a geofence point from a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="pointId">The id of the geofence point to delete</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the geofence point was deleted</returns>
    public ValueTask<OneOf<Success, NotFound>> DeleteGeofencePointAsync(int sessionId, int pointId, bool tracking);

    /// <summary>
    ///     Data used to create or update a geofence point.
    /// </summary>
    /// <param name="SessionId">The id of the session</param>
    /// <param name="Latitude">Latitude in decimal degrees</param>
    /// <param name="Longitude">Longitude in decimal degrees</param>
    /// <param name="SequenceOrder">Order of the point in the geofence polygon</param>
    public sealed record GeofencePointData(
        int SessionId,
        double Latitude,
        double Longitude,
        int SequenceOrder
    );
}

internal sealed class GeoFencePointService(IUnitOfWork uow, ILogger<GeoFencePointService> logger) : IGeofencePointService
{
    public async ValueTask<IReadOnlyCollection<GeofencePoint>> GetAllPointsBySessionIdAsync(int sessionId, bool tracking)
    {
        IEnumerable<GeofencePoint> geofencePoints = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking);

        return [.. geofencePoints];
    }

    public async ValueTask<OneOf<GeofencePoint, NotFound>> GetGeofencePointByIdAsync(int sessionId, int pointId, bool tracking)
    {
        var geofencePoint = await uow.GeofencePointRepository.GetPointBySessionAndIdAsync(sessionId, pointId, tracking);

        return geofencePoint is not null ? geofencePoint : new NotFound();
    }

    /// <summary>
    ///     Maximum number of geofence points allowed per session.
    ///     This value must be kept in sync with the frontend geofence point limit.
    /// </summary>
    private const int MaxGeofencePoints = 10;

    public async ValueTask<OneOf<GeofencePoint, NotFound, DomainError>> AddGeofencePointAsync(IGeofencePointService.GeofencePointData newGeofencePoint)
    {
        var session = await uow.GameSessionRepository.GetSessionByIdAsync(newGeofencePoint.SessionId, tracking: false);
        if (session is null)
        {
            logger.LogWarning("Rejected geofence point creation because session {SessionId} does not exist", newGeofencePoint.SessionId);
            return new NotFound();
        }

        var count = await uow.GeofencePointRepository.CountPointsBySessionIdAsync(newGeofencePoint.SessionId);
        if (count >= MaxGeofencePoints)
        {
            logger.LogWarning("Rejected geofence point creation because session {SessionId} already has {Count} points (max {Max})", newGeofencePoint.SessionId, count, MaxGeofencePoints);
            return DomainError.GeofencePointLimitReached(newGeofencePoint.SessionId, MaxGeofencePoints);
        }

        var geofencePoint = uow.GeofencePointRepository.AddGeofencePoint(
                newGeofencePoint.SessionId,
                newGeofencePoint.Latitude,
                newGeofencePoint.Longitude,
                newGeofencePoint.SequenceOrder);

        await uow.SaveChangesAsync();

        return geofencePoint;
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateGeofencePointAsync(int pointId, IGeofencePointService.GeofencePointData geofencePointData, bool tracking)
    {
        var geofencePoint = await uow.GeofencePointRepository.GetPointBySessionAndIdAsync(geofencePointData.SessionId, pointId, tracking);

        if (geofencePoint is null)
        {
            return new NotFound();
        }

        geofencePoint.Latitude = geofencePointData.Latitude;
        geofencePoint.Longitude = geofencePointData.Longitude;
        geofencePoint.SequenceOrder = geofencePointData.SequenceOrder;

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteGeofencePointAsync(int sessionId, int pointId, bool tracking)
    {
        var point = await uow.GeofencePointRepository.GetPointBySessionAndIdAsync(sessionId, pointId, tracking);

        if (point is null)
        {
            return new NotFound();
        }

        uow.GeofencePointRepository.RemoveGeofencePoint(point);

        await uow.SaveChangesAsync();

        return new Success();
    }
}
