using OneOf;
using OneOf.Types;

namespace XAct.Core.GeofencePoints;

public interface IGeofencePointService
{
    public ValueTask<IReadOnlyCollection<GeofencePoint>> GetAllGeofencePointsAsync();
    public ValueTask<OneOf<GeofencePoint, NotFound>> GetGeofencePointByIdAsync(Guid pointId);
    public ValueTask<OneOf<GeofencePoint, Error>> AddGeofencePointAsync(GeofencePointData newGeofencePoint);
    public ValueTask<OneOf<Success, NotFound>> UpdateGeofencePointAsync(Guid pointId, GeofencePointData geofencePointData);
    public ValueTask<OneOf<Success, NotFound>> DeleteGeofencePointAsync(Guid pointId);

    public sealed record GeofencePointData(
        Guid SessionId,
        double Latitude,
        double Longitude,
        int SequenceOrder
    );
}

public sealed class GeofencePointService(IDataStorage dataStorage) : IGeofencePointService
{
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<GeofencePoint>> GetAllGeofencePointsAsync()
    {
        IEnumerable<GeofencePoint> geofencePoints = await _dataStorage.GetGeofencePointsAsync();

        return [.. geofencePoints];
    }

    public async ValueTask<OneOf<GeofencePoint, NotFound>> GetGeofencePointByIdAsync(Guid pointId)
    {
        var geofencePoint = await GetGeofencePointById(pointId);

        return geofencePoint is not null ? geofencePoint : new NotFound();
    }

    public async ValueTask<OneOf<GeofencePoint, Error>> AddGeofencePointAsync(IGeofencePointService.GeofencePointData newGeofencePoint)
    {
        try
        {
            var geofencePoint = new GeofencePoint
            {
                PointId = Guid.NewGuid(),
                SessionId = newGeofencePoint.SessionId,
                Latitude = newGeofencePoint.Latitude,
                Longitude = newGeofencePoint.Longitude,
                SequenceOrder = newGeofencePoint.SequenceOrder
            };

            await _dataStorage.AddGeofencePointAsync(geofencePoint);

            return geofencePoint;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateGeofencePointAsync(Guid pointId, IGeofencePointService.GeofencePointData geofencePointData)
    {
        var geofencePoint = await GetGeofencePointById(pointId);

        if (geofencePoint is null)
        {
            return new NotFound();
        }

        geofencePoint.SessionId = geofencePointData.SessionId;
        geofencePoint.Latitude = geofencePointData.Latitude;
        geofencePoint.Longitude = geofencePointData.Longitude;
        geofencePoint.SequenceOrder = geofencePointData.SequenceOrder;

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteGeofencePointAsync(Guid pointId)
    {
        var geofencePoint = await GetGeofencePointById(pointId);

        if (geofencePoint is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemoveGeofencePointAsync(geofencePoint);

        return new Success();
    }

    private async ValueTask<GeofencePoint?> GetGeofencePointById(Guid pointId)
    {
        IEnumerable<GeofencePoint> geofencePoints = await _dataStorage.GetGeofencePointsAsync();

        return geofencePoints.FirstOrDefault(gp => gp.PointId == pointId);
    }
}
