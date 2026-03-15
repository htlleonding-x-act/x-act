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
    public ValueTask<OneOf<LocationLog, NotFound, DomainError>> AddLocationLogAsync(LocationLogData newLocationLog);
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateLocationLogAsync(int sessionId, int teamId, int memberId, int logId, LocationLogData locationLogData, bool tracking);
    public ValueTask<OneOf<Success, NotFound, DomainError>> DeleteLocationLogAsync(int sessionId, int teamId, int memberId, int logId, bool tracking);

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

    public async ValueTask<OneOf<LocationLog, NotFound, DomainError>> AddLocationLogAsync(ILocationLogService.LocationLogData newLocationLog)
    {
        try
        {
            OneOf<Success, NotFound, DomainError> validationResult = await ValidateGameplayMutationAsync(newLocationLog.MemberId);

            return await validationResult.Match<ValueTask<OneOf<LocationLog, NotFound, DomainError>>>(
                async _ =>
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

                    logger.LogInformation("Created location log {LogId} for member {MemberId}", log.Id, newLocationLog.MemberId);

                    return log;
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
                log.IsRevealedPosition = locationLogData.IsRevealedPosition;

                await uow.SaveChangesAsync();

                logger.LogInformation("Updated location log {LogId} for member {MemberId}", logId, memberId);

                return new Success();
            },
            notFound => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(notFound),
            domainError => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(domainError)
        );
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> DeleteLocationLogAsync(int sessionId, int teamId, int memberId, int logId, bool tracking)
    {
        OneOf<Success, NotFound, DomainError> validationResult = await ValidateGameplayMutationAsync(sessionId, teamId, memberId);

        return await validationResult.Match<ValueTask<OneOf<Success, NotFound, DomainError>>>(
            async _ =>
            {
                var log = await uow.LocationLogRepository.GetLogByMemberAndIdAsync(memberId, logId, tracking);

                if (log is null)
                {
                    return new NotFound();
                }

                uow.LocationLogRepository.RemoveLocationLog(log);
                await uow.SaveChangesAsync();

                logger.LogInformation("Deleted location log {LogId} for member {MemberId}", logId, memberId);

                return new Success();
            },
            notFound => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(notFound),
            domainError => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(domainError)
        );
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
}
