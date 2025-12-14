using OneOf;
using OneOf.Types;

namespace XAct.Core.LocationLogs;

public interface ILocationLogService
{
    public ValueTask<IReadOnlyCollection<LocationLog>> GetAllLocationLogsAsync();
    public ValueTask<OneOf<LocationLog, NotFound>> GetLocationLogByIdAsync(Guid logId);
    public ValueTask<OneOf<LocationLog, Error>> AddLocationLogAsync(LocationLogData newLocationLog);
    public ValueTask<OneOf<Success, NotFound>> UpdateLocationLogAsync(Guid logId, LocationLogData locationLogData);
    public ValueTask<OneOf<Success, NotFound>> DeleteLocationLogAsync(Guid logId);

    public sealed record LocationLogData(
        Guid MemberId,
        Instant Timestamp,
        double Latitude,
        double Longitude,
        double AccuracyMeters,
        TransportMode TransportMode,
        bool IsRevealedPosition = false
    );
}

public sealed class LocationLogService(IDataStorage dataStorage) : ILocationLogService
{
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<LocationLog>> GetAllLocationLogsAsync()
    {
        IEnumerable<LocationLog> locationLogs = await _dataStorage.GetLocationLogsAsync();

        return [.. locationLogs];
    }

    public async ValueTask<OneOf<LocationLog, NotFound>> GetLocationLogByIdAsync(Guid logId)
    {
        var locationLog = await GetLocationLogById(logId);

        return locationLog is not null ? locationLog : new NotFound();
    }

    public async ValueTask<OneOf<LocationLog, Error>> AddLocationLogAsync(ILocationLogService.LocationLogData newLocationLog)
    {
        try
        {
            var locationLog = new LocationLog
            {
                LogId = Guid.NewGuid(),
                MemberId = newLocationLog.MemberId,
                Timestamp = newLocationLog.Timestamp,
                Latitude = newLocationLog.Latitude,
                Longitude = newLocationLog.Longitude,
                AccuracyMeters = newLocationLog.AccuracyMeters,
                TransportMode = newLocationLog.TransportMode,
                IsRevealedPosition = newLocationLog.IsRevealedPosition
            };

            await _dataStorage.AddLocationLogAsync(locationLog);

            return locationLog;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateLocationLogAsync(Guid logId, ILocationLogService.LocationLogData locationLogData)
    {
        var locationLog = await GetLocationLogById(logId);

        if (locationLog is null)
        {
            return new NotFound();
        }

        locationLog.MemberId = locationLogData.MemberId;
        locationLog.Timestamp = locationLogData.Timestamp;
        locationLog.Latitude = locationLogData.Latitude;
        locationLog.Longitude = locationLogData.Longitude;
        locationLog.AccuracyMeters = locationLogData.AccuracyMeters;
        locationLog.TransportMode = locationLogData.TransportMode;
        locationLog.IsRevealedPosition = locationLogData.IsRevealedPosition;

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteLocationLogAsync(Guid logId)
    {
        var locationLog = await GetLocationLogById(logId);

        if (locationLog is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemoveLocationLogAsync(locationLog);

        return new Success();
    }

    private async ValueTask<LocationLog?> GetLocationLogById(Guid logId)
    {
        IEnumerable<LocationLog> locationLogs = await _dataStorage.GetLocationLogsAsync();

        return locationLogs.FirstOrDefault(ll => ll.LogId == logId);
    }
}
