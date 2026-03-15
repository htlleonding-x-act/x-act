using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ILocationLogService
{
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking);
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsBySessionIdAsync(int sessionId, bool tracking);
    public ValueTask<OneOf<LocationLog, NotFound>> GetLocationLogByIdAsync(int sessionId, int teamId, int memberId, int logId, bool tracking);
    public ValueTask<OneOf<LocationLog, Error>> AddLocationLogAsync(LocationLogData newLocationLog);
    public ValueTask<OneOf<Success, NotFound>> UpdateLocationLogAsync(int sessionId, int teamId, int memberId, int logId, LocationLogData locationLogData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteLocationLogAsync(int sessionId, int teamId, int memberId, int logId, bool tracking);

    public sealed record LocationLogData(
        int MemberId,
        Instant Timestamp,
        double Latitude,
        double Longitude,
        double AccuracyMeters,
        TransportMode TransportMode,
        bool IsRevealedPosition = false
    );
}

internal sealed class LocationLogService(IUnitOfWork uow, ILogger<LocationLogService> logger) : ILocationLogService
{
    public async ValueTask<IReadOnlyCollection<LocationLog>> GetLogsByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return [];
        }

        IEnumerable<LocationLog> logs = await uow.LocationLogRepository.GetLogsByMemberIdAsync(memberId, tracking);
        return [.. logs];
    }

    public async ValueTask<IReadOnlyCollection<LocationLog>> GetLogsBySessionIdAsync(int sessionId, bool tracking)
    {
        IEnumerable<LocationLog> logs = await uow.LocationLogRepository.GetLogsBySessionIdAsync(sessionId, tracking);
        return [.. logs];
    }

    public async ValueTask<OneOf<LocationLog, NotFound>> GetLocationLogByIdAsync(int sessionId, int teamId, int memberId, int logId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var log = await uow.LocationLogRepository.GetLogByMemberAndIdAsync(memberId, logId, tracking);

        return log is not null ? log : new NotFound();
    }

    public async ValueTask<OneOf<LocationLog, Error>> AddLocationLogAsync(ILocationLogService.LocationLogData newLocationLog)
    {
        try
        {
            var log = uow.LocationLogRepository.AddLocationLog(
                newLocationLog.MemberId,
                newLocationLog.Timestamp,
                newLocationLog.Latitude,
                newLocationLog.Longitude,
                newLocationLog.AccuracyMeters,
                newLocationLog.TransportMode,
                newLocationLog.IsRevealedPosition);

            await uow.SaveChangesAsync();

            return log;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add location log for member {MemberId}", newLocationLog.MemberId);
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateLocationLogAsync(int sessionId, int teamId, int memberId, int logId, ILocationLogService.LocationLogData locationLogData, bool tracking)
    {
        if (locationLogData.MemberId != memberId)
        {
            return new NotFound();
        }

        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var log = await uow.LocationLogRepository.GetLogByMemberAndIdAsync(memberId, logId, tracking);

        if (log is null)
        {
            return new NotFound();
        }

        log.Timestamp = locationLogData.Timestamp;
        log.Latitude = locationLogData.Latitude;
        log.Longitude = locationLogData.Longitude;
        log.AccuracyMeters = locationLogData.AccuracyMeters;
        log.TransportMode = locationLogData.TransportMode;
        log.IsRevealedPosition = locationLogData.IsRevealedPosition;

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteLocationLogAsync(int sessionId, int teamId, int memberId, int logId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var log = await uow.LocationLogRepository.GetLogByMemberAndIdAsync(memberId, logId, tracking);

        if (log is null)
        {
            return new NotFound();
        }

        uow.LocationLogRepository.RemoveLocationLog(log);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
