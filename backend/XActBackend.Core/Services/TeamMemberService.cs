using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Provides methods to manage team members in a session.
/// </summary>
public interface ITeamMemberService
{
    /// <summary>
    ///     Get all members of a team in a session by the teams id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All team members for the team</returns>
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int sessionId, int teamId, bool tracking);

    /// <summary>
    ///     Get a team member by id for a team in a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The member, if found</returns>
    public ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int sessionId, int teamId, int memberId, bool tracking);

    /// <summary>
    ///     Add a new team member.
    /// </summary>
    /// <param name="newTeamMember">The member data to create</param>
    /// <returns>The created team member, not found or a domain error if validation fails</returns>
    public ValueTask<OneOf<TeamMember, NotFound, DomainError>> AddTeamMemberAsync(TeamMemberData newTeamMember);

    /// <summary>
    ///     Update an existing team member.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member to update</param>
    /// <param name="teamMemberData">The new member data</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the update was successful</returns>
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateTeamMemberAsync(int sessionId, int teamId, int memberId, TeamMemberData teamMemberData, bool tracking);

    /// <summary>
    ///     Delete a team member.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member to delete</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>Result indicating if the member was deleted</returns>
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int sessionId, int teamId, int memberId, bool tracking);

    /// <summary>
    ///     Data used to create or update a team member.
    /// </summary>
    /// <param name="SessionId">The id of the session</param>
    /// <param name="TeamId">The id of the team</param>
    /// <param name="UserId">Optional user id when the member is a registered user</param>
    /// <param name="GuestName">Optional guest name when the member is not a registered user</param>
    /// <param name="IsTeamLeader">Flag indicating if the member is team leader</param>
    /// <param name="CurrentLatitude">Optional current latitude</param>
    /// <param name="CurrentLongitude">Optional current longitude</param>
    /// <param name="LastUpdated">Optional timestamp for the last position update</param>
    public sealed record TeamMemberData(
        int SessionId,
        int TeamId,
        string? UserId,
        string? GuestName,
        bool IsTeamLeader = false,
        double? CurrentLatitude = null,
        double? CurrentLongitude = null,
        Instant? LastUpdated = null
    );
}

internal sealed class TeamMemberService(IUnitOfWork uow, IClock clock, ILogger<TeamMemberService> logger) : ITeamMemberService
{
    public async ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int sessionId, int teamId, bool tracking)
    {
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionAndTeamIdAsync(sessionId, teamId, tracking);

        return members;
    }

    public async ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking);

        return member is not null ? member : new NotFound();
    }

    public async ValueTask<OneOf<TeamMember, NotFound, DomainError>> AddTeamMemberAsync(ITeamMemberService.TeamMemberData newTeamMember)
    {
        try
        {
            OneOf<NotFound, DomainError, Success> validationResult = await ValidateMemberMutationAsync(
                newTeamMember.SessionId,
                newTeamMember.TeamId,
                newTeamMember.UserId,
                newTeamMember.GuestName,
                newTeamMember.IsTeamLeader,
                currentMemberId: null);

            return await validationResult.Match<ValueTask<OneOf<TeamMember, NotFound, DomainError>>>(
                notFound => ValueTask.FromResult<OneOf<TeamMember, NotFound, DomainError>>(notFound),
                domainError => ValueTask.FromResult<OneOf<TeamMember, NotFound, DomainError>>(domainError),
                async _ =>
                {
                    var member = uow.TeamMemberRepository.AddTeamMember(
                        newTeamMember.SessionId,
                        newTeamMember.TeamId,
                        newTeamMember.UserId,
                        newTeamMember.GuestName,
                        newTeamMember.IsTeamLeader
                    );

                    member.CurrentLatitude = newTeamMember.CurrentLatitude;
                    member.CurrentLongitude = newTeamMember.CurrentLongitude;
                    member.LastUpdated = newTeamMember.LastUpdated ?? clock.GetCurrentInstant();

                    await uow.SaveChangesAsync();

                    logger.LogInformation("Created team member {MemberId} in session {SessionId}, team {TeamId}", member.Id, newTeamMember.SessionId, newTeamMember.TeamId);

                    return member;
                }
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add team member in session {SessionId}, team {TeamId}", newTeamMember.SessionId, newTeamMember.TeamId);
            throw;
        }
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> UpdateTeamMemberAsync(int sessionId, int teamId, int memberId, ITeamMemberService.TeamMemberData teamMemberData, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking);

        if (member is null || teamMemberData.SessionId != sessionId || teamMemberData.TeamId != teamId)
        {
            return new NotFound();
        }

        OneOf<NotFound, DomainError, Success> validationResult = await ValidateMemberMutationAsync(
            sessionId,
            teamId,
            teamMemberData.UserId,
            teamMemberData.GuestName,
            teamMemberData.IsTeamLeader,
            currentMemberId: memberId);

        return await validationResult.Match<ValueTask<OneOf<Success, NotFound, DomainError>>>(
            notFound => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(notFound),
            domainError => ValueTask.FromResult<OneOf<Success, NotFound, DomainError>>(domainError),
            async _ =>
            {
                member.TeamId = teamMemberData.TeamId;
                member.SessionId = teamMemberData.SessionId;
                member.UserId = teamMemberData.UserId;
                member.GuestName = teamMemberData.GuestName;
                member.IsTeamLeader = teamMemberData.IsTeamLeader;
                member.CurrentLatitude = teamMemberData.CurrentLatitude;
                member.CurrentLongitude = teamMemberData.CurrentLongitude;
                member.LastUpdated = teamMemberData.LastUpdated ?? clock.GetCurrentInstant();

                await uow.SaveChangesAsync();

                logger.LogInformation("Updated team member {MemberId} in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);

                return new Success();
            }
        );
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking);

        if (member is null)
        {
            return new NotFound();
        }

        uow.TeamMemberRepository.RemoveTeamMember(member);
        await uow.SaveChangesAsync();

        logger.LogInformation("Deleted team member {MemberId} from session {SessionId}, team {TeamId}", memberId, sessionId, teamId);

        return new Success();
    }

    private async ValueTask<OneOf<NotFound, DomainError, Success>> ValidateMemberMutationAsync(
        int sessionId,
        int teamId,
        string? userId,
        string? guestName,
        bool isTeamLeader,
        int? currentMemberId)
    {
        bool hasUser = !string.IsNullOrWhiteSpace(userId);
        bool hasGuest = !string.IsNullOrWhiteSpace(guestName);
        if (hasUser == hasGuest)
        {
            logger.LogWarning("Rejected team member mutation in session {SessionId}, team {TeamId} because identity is invalid", sessionId, teamId);
            return DomainError.InvalidMemberIdentity();
        }

        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            logger.LogWarning("Rejected team member mutation because session {SessionId} does not exist", sessionId);
            return new NotFound();
        }

        if (session.Status != SessionStatus.Waiting)
        {
            logger.LogWarning("Rejected team member mutation in session {SessionId} because status is {Status}", sessionId, session.Status);
            return DomainError.SessionNotJoinable(sessionId, session.Status);
        }

        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking: false);
        if (team is null)
        {
            logger.LogWarning("Rejected team member mutation because team {TeamId} does not exist", teamId);
            return new NotFound();
        }

        if (team.SessionId != sessionId)
        {
            logger.LogWarning("Rejected team member mutation because team {TeamId} does not belong to session {SessionId}", teamId, sessionId);
            return DomainError.TeamNotInSession(teamId, sessionId);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await uow.UserRepository.GetUserByIdAsync(userId, tracking: false);
            if (user is null)
            {
                logger.LogWarning("Rejected team member mutation because user {UserId} does not exist", userId);
                return new NotFound();
            }

            if (user.IsDeleted)
            {
                logger.LogWarning("Rejected team member mutation because user {UserId} is deleted", userId);
                return DomainError.UserDeleted(userId);
            }

            var existingMember = await uow.TeamMemberRepository.GetMemberBySessionAndUserIdAsync(sessionId, userId, tracking: false);
            if (existingMember is not null && existingMember.Id != currentMemberId)
            {
                logger.LogWarning("Rejected team member mutation because user {UserId} is already part of session {SessionId}", userId, sessionId);
                return DomainError.UserAlreadyJoined(userId, sessionId);
            }
        }

        if (isTeamLeader)
        {
            IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionAndTeamIdAsync(sessionId, teamId, tracking: false);
            bool hasOtherLeader = members.Any(member => member.IsTeamLeader && member.Id != currentMemberId);
            if (hasOtherLeader)
            {
                logger.LogWarning("Rejected team member mutation because team {TeamId} already has another leader", teamId);
                return DomainError.TeamLeaderAlreadyExists(teamId);
            }
        }

        return new Success();
    }
}
