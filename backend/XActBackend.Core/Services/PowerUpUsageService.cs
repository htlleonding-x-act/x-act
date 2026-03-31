using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Provides methods to manage power-up usage events for team members.
/// </summary>
public interface IPowerUpUsageService
{
    /// <summary>
    ///     Get all power-up usages for a member of a team in a session by member id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All power-up usages for the member</returns>
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking);

    /// <summary>
    ///     Get a power-up usage by id of the event for a member of a team in a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member</param>
    /// <param name="usageId">The id of the usage event</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The power-up usage or not found if validation fails</returns>
    public ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking);

    /// <summary>
    ///     Add a new power-up usage event.
    /// </summary>
    /// <param name="newPowerUpUsage">The power-up usage data to create</param>
    /// <returns>The created usage, not found or a domain error if validation fails</returns>
    public ValueTask<OneOf<PowerUpUsage, NotFound, DomainError>> AddPowerUpUsageAsync(PowerUpUsageData newPowerUpUsage);

    /// <summary>
    ///     Update an existing power-up usage event.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member</param>
    /// <param name="usageId">The id of the usage event to update</param>
    /// <param name="powerUpUsageData">The new power-up usage data</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the update was successful</returns>
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdatePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, PowerUpUsageData powerUpUsageData, bool tracking);

    /// <summary>
    ///     Delete a power-up usage event.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member</param>
    /// <param name="usageId">The id of the usage event to delete</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the usage was deleted</returns>
    public ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking);

    /// <summary>
    ///     Data used to create or update a power-up usage event.
    /// </summary>
    /// <param name="MemberId">The id of the team member</param>
    /// <param name="PowerUpType">The type of power-up used</param>
    /// <param name="UsedAt">Timestamp when the power-up was used</param>
    public sealed record PowerUpUsageData(
        int MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );
}

internal sealed class PowerUpUsageService(IUnitOfWork uow, ILogger<PowerUpUsageService> logger) : IPowerUpUsageService
{
    public async ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return [];
        }

        IEnumerable<PowerUpUsage> powerUpUsages = await uow.PowerUpUsageRepository.GetUsagesByMemberIdAsync(memberId, tracking);
        return [.. powerUpUsages];
    }

    public async ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var powerUpUsage = await uow.PowerUpUsageRepository.GetUsageByMemberAndIdAsync(memberId, usageId, tracking);

        return powerUpUsage is not null ? powerUpUsage : new NotFound();
    }

    public async ValueTask<OneOf<PowerUpUsage, NotFound, DomainError>> AddPowerUpUsageAsync(IPowerUpUsageService.PowerUpUsageData newPowerUpUsage)
    {
        try
        {
            OneOf<Success, NotFound, DomainError> validationResult = await ValidatePowerUpMutationAsync(newPowerUpUsage.MemberId, newPowerUpUsage.PowerUpType);

            return await validationResult.Match<ValueTask<OneOf<PowerUpUsage, NotFound, DomainError>>>(
                async _ =>
                {
                    var powerUpUsage = uow.PowerUpUsageRepository.AddPowerUpUsage(
                        newPowerUpUsage.MemberId,
                        newPowerUpUsage.PowerUpType,
                        newPowerUpUsage.UsedAt
                    );

                    await uow.SaveChangesAsync();

                    logger.LogInformation("Created power-up usage {UsageId} for member {MemberId}", powerUpUsage.Id, newPowerUpUsage.MemberId);

                    return powerUpUsage;
                },
                notFound => ValueTask.FromResult<OneOf<PowerUpUsage, NotFound, DomainError>>(notFound),
                domainError => ValueTask.FromResult<OneOf<PowerUpUsage, NotFound, DomainError>>(domainError)
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add power-up usage for member {MemberId}", newPowerUpUsage.MemberId);
            throw;
        }
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> UpdatePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, IPowerUpUsageService.PowerUpUsageData powerUpUsageData, bool tracking)
    {
        if (powerUpUsageData.MemberId != memberId)
        {
            return new NotFound();
        }

        OneOf<Success, NotFound, DomainError> validationResult = await ValidatePowerUpMutationAsync(sessionId, teamId, memberId, powerUpUsageData.PowerUpType);

        return await validationResult.Match<ValueTask<OneOf<Success, NotFound, DomainError>>>(
            async _ =>
            {
                var powerUpUsage = await uow.PowerUpUsageRepository.GetUsageByMemberAndIdAsync(memberId, usageId, tracking);

                if (powerUpUsage is null)
                {
                    return new NotFound();
                }

                powerUpUsage.PowerUpType = powerUpUsageData.PowerUpType;
                powerUpUsage.UsedAt = powerUpUsageData.UsedAt;

                await uow.SaveChangesAsync();

                logger.LogInformation("Updated power-up usage {UsageId} for member {MemberId}", usageId, memberId);

                return new Success();
            },
            notFound => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(notFound),
            domainError => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(domainError)
        );
    }

    public async ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            logger.LogWarning("Rejected power-up usage delete because member {MemberId} was not found in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
            return new NotFound();
        }

        var usage = await uow.PowerUpUsageRepository.GetUsageByMemberAndIdAsync(memberId, usageId, tracking);
        if (usage is null)
        {
            logger.LogWarning("Rejected power-up usage delete because usage {UsageId} was not found for member {MemberId}", usageId, memberId);
            return new NotFound();
        }

        uow.PowerUpUsageRepository.RemovePowerUpUsage(usage);
        await uow.SaveChangesAsync();

        logger.LogInformation("Deleted power-up usage {UsageId} for member {MemberId}", usageId, memberId);

        return new Success();
    }

    private async ValueTask<OneOf<Success, NotFound, DomainError>> ValidatePowerUpMutationAsync(int memberId, PowerUpType? powerUpType)
    {
        var member = await uow.TeamMemberRepository.GetMemberByIdAsync(memberId, tracking: false);
        if (member is null)
        {
            logger.LogWarning("Rejected power-up mutation because member {MemberId} does not exist", memberId);
            return new NotFound();
        }

        return await ValidatePowerUpMutationAsync(member.SessionId, member.TeamId, memberId, powerUpType);
    }

    private async ValueTask<OneOf<Success, NotFound, DomainError>> ValidatePowerUpMutationAsync(int sessionId, int teamId, int memberId, PowerUpType? powerUpType)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            logger.LogWarning("Rejected power-up mutation because member {MemberId} does not exist in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);
            return new NotFound();
        }

        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            logger.LogWarning("Rejected power-up mutation because session {SessionId} does not exist", sessionId);
            return new NotFound();
        }

        if (session.Status != SessionStatus.Active)
        {
            logger.LogWarning("Rejected power-up mutation for member {MemberId} because session {SessionId} is in status {Status}", memberId, sessionId, session.Status);
            return DomainError.SessionNotActive(sessionId, session.Status);
        }

        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking: false);
        if (team is null)
        {
            logger.LogWarning("Rejected power-up mutation because team {TeamId} does not exist", teamId);
            return new NotFound();
        }

        if (team.Role != TeamRole.MrX)
        {
            logger.LogWarning("Rejected power-up mutation for member {MemberId} because team {TeamId} has role {Role}", memberId, teamId, team.Role);
            return DomainError.PowerUpNotAllowedForTeamRole(powerUpType ?? PowerUpType.BlackTicket, team.Role);
        }

        return new Success();
    }
}
