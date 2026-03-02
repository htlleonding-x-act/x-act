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

internal sealed class GeoFencePointService(IUnitOfWork uow) : IGeofencePointService
{
    public async ValueTask<IReadOnlyCollection<GeofencePoint>> GetAllPointsBySessionIdAsync(int sessionId, bool tracking)
    {
        IEnumerable<GeofencePoint> geofencePoints = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking);

        return [.. geofencePoints];
    }

    public async ValueTask<OneOf<GeofencePoint, NotFound>> GetGeofencePointByIdAsync(int sessionId, int pointId, bool tracking)
    {
        IReadOnlyCollection<GeofencePoint> geofencePoints = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking);
        var geofencePoint = geofencePoints.FirstOrDefault(p => p.Id == pointId);

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
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateGeofencePointAsync(int pointId, IGeofencePointService.GeofencePointData geofencePointData, bool tracking)
    {
        IReadOnlyCollection<GeofencePoint> geofencePoints = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(geofencePointData.SessionId, tracking);
        var geofencePoint = geofencePoints.FirstOrDefault(p => p.Id == pointId);

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
        IReadOnlyCollection<GeofencePoint> geofencePoints = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking);
        var point = geofencePoints.FirstOrDefault(p => p.Id == pointId);

        if (point is null)
        {
            return new NotFound();
        }

        uow.GeofencePointRepository.RemoveGeofencePoint(point);

        await uow.SaveChangesAsync();

        return new Success();
    }
}
