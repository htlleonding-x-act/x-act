using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="LocationLog"/> entities.
/// </summary>
public interface ILocationLogRepository
{
    /// <summary>
    ///     Add a new location log.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="timestamp">Timestamp of the logged position</param>
    /// <param name="latitude">Latitude in decimal degrees</param>
    /// <param name="longitude">Longitude in decimal degrees</param>
    /// <param name="accuracyMeters">Position accuracy in meters</param>
    /// <param name="transportMode">Transport mode</param>
    /// <param name="isRevealedPosition">Flag indicating if this is a revealed position</param>
    /// <returns>The created location log entity</returns>
    public LocationLog AddLocationLog(
        int memberId,
        Instant timestamp,
        double latitude,
        double longitude,
        double accuracyMeters,
        TransportMode transportMode,
        bool isRevealedPosition
    );

    /// <summary>
    ///     Get all location logs for a member.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All location logs for the member</returns>
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsByMemberIdAsync(int memberId, bool tracking);

    /// <summary>
    ///     Get all location logs for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All location logs for the session</returns>
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsBySessionIdAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Get a location log by member id and log id.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="logId">The id of the location log</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The location log, if found</returns>
    public ValueTask<LocationLog?> GetLogByMemberAndIdAsync(int memberId, int logId, bool tracking);

    /// <summary>
    ///     Remove a location log from the repository.
    /// </summary>
    /// <param name="log">The location log to remove</param>
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

    public async ValueTask<LocationLog?> GetLogByMemberAndIdAsync(int memberId, int logId, bool tracking)
    {
        IQueryable<LocationLog> source = tracking ? Logs : LogsNoTracking;

        return await source.FirstOrDefaultAsync(l => l.MemberId == memberId && l.Id == logId);
    }

    public void RemoveLocationLog(LocationLog log)
    {
        logSet.Remove(log);
    }
}
