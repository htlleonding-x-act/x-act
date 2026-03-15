using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IGeofencePointService
{
    public ValueTask<IReadOnlyCollection<GeofencePoint>> GetAllPointsBySessionIdAsync(int sessionId, bool tracking);
    public ValueTask<OneOf<GeofencePoint, NotFound>> GetGeofencePointByIdAsync(int sessionId, int pointId, bool tracking);
    public ValueTask<OneOf<GeofencePoint, Error>> AddGeofencePointAsync(GeofencePointData newGeofencePoint);
    public ValueTask<OneOf<Success, NotFound>> UpdateGeofencePointAsync(int pointId, GeofencePointData geofencePointData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteGeofencePointAsync(int sessionId, int pointId, bool tracking);

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

    public async ValueTask<OneOf<GeofencePoint, Error>> AddGeofencePointAsync(IGeofencePointService.GeofencePointData newGeofencePoint)
    {
        try
        {
            var geofencePoint = uow.GeofencePointRepository.AddGeofencePoint(
                    newGeofencePoint.SessionId,
                    newGeofencePoint.Latitude,
                    newGeofencePoint.Longitude,
                    newGeofencePoint.SequenceOrder);

            await uow.SaveChangesAsync();

            return geofencePoint;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add geofence point for session {SessionId}", newGeofencePoint.SessionId);
            return new Error();
        }
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
