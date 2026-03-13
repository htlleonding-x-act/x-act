using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface ILocationLogRepository
{
    public LocationLog AddLocationLog(
        int memberId,
        Instant timestamp,
        double latitude,
        double longitude,
        double accuracyMeters,
        TransportMode transportMode,
        bool isRevealedPosition
    );
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsByMemberIdAsync(int memberId, bool tracking);
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsBySessionIdAsync(int sessionId, bool tracking);
    public void RemoveLocationLog(LocationLog log);
}

internal sealed class LocationLogRepository(DbSet<LocationLog> logSet) : ILocationLogRepository
{
    private IQueryable<LocationLog> Logs => logSet;
    private IQueryable<LocationLog> LogsNoTracking => Logs.AsNoTracking();

    public LocationLog AddLocationLog(
        int memberId,
        Instant timestamp,
        double latitude,
        double longitude,
        double accuracyMeters,
        TransportMode transportMode,
        bool isRevealedPosition
    )
    {
        var log = new LocationLog
        {
            MemberId = memberId,
            Timestamp = timestamp,
            Latitude = latitude,
            Longitude = longitude,
            AccuracyMeters = accuracyMeters,
            TransportMode = transportMode,
            IsRevealedPosition = isRevealedPosition,
        };

        logSet.Add(log);

        return log;
    }

    public async ValueTask<IReadOnlyCollection<LocationLog>> GetLogsByMemberIdAsync(int memberId, bool tracking)
    {
        IQueryable<LocationLog> source = tracking ? Logs : LogsNoTracking;

        List<LocationLog> logs = await source
            .Where(l => l.MemberId == memberId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        return logs;
    }

    public async ValueTask<IReadOnlyCollection<LocationLog>> GetLogsBySessionIdAsync(int sessionId, bool tracking)
    {
        IQueryable<LocationLog> source = tracking ? Logs : LogsNoTracking;

        List<LocationLog> logs = await source
            .Where(l => l.Member.SessionId == sessionId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync();

        return logs;
    }

    public void RemoveLocationLog(LocationLog log)
    {
        logSet.Remove(log);
    }
}
