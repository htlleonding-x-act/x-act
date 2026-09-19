using OneOf;
using OneOf.Types;
using System.Collections.Concurrent;
using System.Threading;
using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ILocationLogService
{
    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking);

    public ValueTask<IReadOnlyCollection<LocationLog>> GetLogsBySessionIdAsync(int sessionId, bool tracking);

    public ValueTask<OneOf<LocationLog, NotFound>> GetLocationLogByIdAsync(int sessionId, int teamId, int memberId, int logId, bool tracking);

    /// <summary>
    ///     the server picks the stored timestamp and whether the ping is a reveal, so <c>Timestamp</c> and
    ///     <c>IsRevealedPosition</c> from the client are ignored here
    /// </summary>
    public ValueTask<OneOf<LocationLog, NotFound, DomainError>> AddLocationLogAsync(LocationLogData newLocationLog);

    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateLocationLogAsync(int sessionId, int teamId, int memberId, int logId, LocationLogData locationLogData, bool tracking);

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

internal sealed class LocationLogService(IUnitOfWork uow, IClock clock, ILogger<LocationLogService> logger) : ILocationLogService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> member_Locks = new();

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

    public async ValueTask<OneOf<LocationLog, NotFound, DomainError>> AddLocationLogAsync(ILocationLogService.LocationLogData newLocationLog)
    {
        try
        {
            OneOf<Success, NotFound, DomainError> validationResult = await ValidateGameplayMutationAsync(newLocationLog.MemberId);

            return await validationResult.Match<ValueTask<OneOf<LocationLog, NotFound, DomainError>>>(
                async _ =>
                {
                    // one lock per member so two pings arriving together can't both become the reveal ping.
                    // the locks are static, so this only holds within a single server process
                    SemaphoreSlim memberLock = member_Locks.GetOrAdd(newLocationLog.MemberId, _ => new SemaphoreSlim(1, 1));
                    await memberLock.WaitAsync();
                    try
                    {
                        // server time drives both the reveal check and the stored timestamp. a client timestamp
                        // could force a reveal window, and phone clock skew or network delay could put a ping
                        // into the wrong window and reveal mr.x twice
                        Instant serverNow = clock.GetCurrentInstant();
                        bool isRevealed = await DetermineIfRevealedPositionAsync(newLocationLog.MemberId, serverNow);

                        var log = uow.LocationLogRepository.AddLocationLog(
                            newLocationLog.MemberId,
                            serverNow,
                            newLocationLog.Latitude,
                            newLocationLog.Longitude,
                            newLocationLog.AccuracyMeters,
                            newLocationLog.TransportMode,
                            isRevealed);

                        await uow.SaveChangesAsync();

                        logger.LogInformation("Created location log {LogId} for member {MemberId} (revealed: {IsRevealed})", log.Id, newLocationLog.MemberId, isRevealed);

                        return log;
                    }
                    finally
                    {
                        memberLock.Release();
                    }
                },
                notFound => ValueTask.FromResult<OneOf<LocationLog, NotFound, DomainError>>(notFound),
                domainError => ValueTask.FromResult<OneOf<LocationLog, NotFound, DomainError>>(domainError)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add location log for member {MemberId}", newLocationLog.MemberId);
            throw;
        }
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> UpdateLocationLogAsync(int sessionId, int teamId, int memberId, int logId, ILocationLogService.LocationLogData locationLogData, bool tracking)
    {
        if (locationLogData.MemberId != memberId)
        {
            return new NotFound();
        }

        OneOf<Success, NotFound, DomainError> validationResult = await ValidateGameplayMutationAsync(sessionId, teamId, memberId);

        return await validationResult.Match<ValueTask<OneOf<Success, NotFound, DomainError>>>(
            async _ =>
            {
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

                await uow.SaveChangesAsync();

                logger.LogInformation("Updated location log {LogId} for member {MemberId}", logId, memberId);

                return new Success();
            },
            notFound => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(notFound),
            domainError => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(domainError)
        );
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteLocationLogAsync(int sessionId, int teamId, int memberId, int logId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            logger.LogWarning("Rejected location log delete because member {MemberId} was not found in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
            return new NotFound();
        }

        var log = await uow.LocationLogRepository.GetLogByMemberAndIdAsync(memberId, logId, tracking);
        if (log is null)
        {
            logger.LogWarning("Rejected location log delete because log {LogId} was not found for member {MemberId}", logId, memberId);
            return new NotFound();
        }

        uow.LocationLogRepository.RemoveLocationLog(log);
        await uow.SaveChangesAsync();

        logger.LogInformation("Deleted location log {LogId} for member {MemberId}", logId, memberId);

        return new Success();
    }

    private async ValueTask<OneOf<Success, NotFound, DomainError>> ValidateGameplayMutationAsync(int memberId)
    {
        var member = await uow.TeamMemberRepository.GetMemberByIdAsync(memberId, tracking: false);
        if (member is null)
        {
            logger.LogWarning("Rejected gameplay mutation because member {MemberId} does not exist", memberId);
            return new NotFound();
        }

        return await ValidateGameplayMutationAsync(member.SessionId, member.TeamId, memberId);
    }

    private async ValueTask<OneOf<Success, NotFound, DomainError>> ValidateGameplayMutationAsync(int sessionId, int teamId, int memberId)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            logger.LogWarning("Rejected gameplay mutation because member {MemberId} does not exist in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
            return new NotFound();
        }

        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            logger.LogWarning("Rejected gameplay mutation because session {SessionId} does not exist", sessionId);
            return new NotFound();
        }

        if (session.Status != SessionStatus.Active)
        {
            logger.LogWarning("Rejected gameplay mutation for member {MemberId} because session {SessionId} is in status {Status}", memberId, sessionId, session.Status);
            return DomainError.SessionNotActive(sessionId, session.Status);
        }

        return new Success();
    }

    private async ValueTask<bool> DetermineIfRevealedPositionAsync(int memberId, Instant logTimestamp)
    {
        try
        {
            var member = await uow.TeamMemberRepository.GetMemberByIdAsync(memberId, tracking: false);
            if (member is null)
            {
                return false;
            }

            var team = await uow.TeamRepository.GetTeamByIdAsync(member.TeamId, tracking: false);
            if (team is null || team.Role != TeamRole.MrX)
            {
                return false;
            }

            var session = await uow.GameSessionRepository.GetSessionByIdAsync(member.SessionId, tracking: false);
            if (session is null || session.StartTime is null || session.Status != SessionStatus.Active)
            {
                return false;
            }

            if (!RevealTimingCalculator.TryGetRevealWindow(
                    session.StartTime.Value,
                    logTimestamp,
                    session.MrXRevealInterval,
                    out var intervalStart,
                    out var intervalEnd,
                    out _,
                    out _))
            {
                return false;
            }

            // the first mr.x ping in each interval is the reveal ping
            IEnumerable<LocationLog> memberLogs = await uow.LocationLogRepository.GetLogsByMemberIdAsync(memberId, tracking: false);
            var alreadyRevealedInInterval = memberLogs.Any(log =>
                log.IsRevealedPosition &&
                log.Timestamp >= intervalStart &&
                log.Timestamp < intervalEnd);

            return !alreadyRevealedInInterval;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to determine if position should be revealed for member {MemberId}", memberId);
            return false;
        }
    }
}
