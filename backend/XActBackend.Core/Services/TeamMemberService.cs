using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ITeamMemberService
{
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int sessionId, int teamId, bool tracking);
    public ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int sessionId, int teamId, int memberId, bool tracking);
    public ValueTask<OneOf<TeamMember, NotFound, DomainError>> AddTeamMemberAsync(TeamMemberData newTeamMember);
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateTeamMemberAsync(int sessionId, int teamId, int memberId, TeamMemberData teamMemberData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int sessionId, int teamId, int memberId, bool tracking);

    public sealed record TeamMemberData(
        int SessionId,
        int TeamId,
        int? UserId,
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
        int? userId,
        string? guestName,
        bool isTeamLeader,
        int? currentMemberId)
    {
        bool hasUser = userId.HasValue;
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

        if (userId.HasValue)
        {
            var user = await uow.UserRepository.GetUserByIdAsync(userId.Value, tracking: false);
            if (user is null)
            {
                logger.LogWarning("Rejected team member mutation because user {UserId} does not exist", userId.Value);
                return new NotFound();
            }

            if (user.IsDeleted)
            {
                logger.LogWarning("Rejected team member mutation because user {UserId} is deleted", userId.Value);
                return DomainError.UserDeleted(userId.Value);
            }

            var existingMember = await uow.TeamMemberRepository.GetMemberBySessionAndUserIdAsync(sessionId, userId.Value, tracking: false);
            if (existingMember is not null && existingMember.Id != currentMemberId)
            {
                logger.LogWarning("Rejected team member mutation because user {UserId} is already part of session {SessionId}", userId.Value, sessionId);
                return DomainError.UserAlreadyJoined(userId.Value, sessionId);
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
