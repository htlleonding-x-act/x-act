using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ITeamService
{
    public ValueTask<IReadOnlyCollection<Team>> GetTeamsBySessionIdAsync(int sessionId, bool tracking);
    public ValueTask<OneOf<Team, NotFound>> GetTeamByIdAsync(int sessionId, int teamId, bool tracking);
    public ValueTask<OneOf<Team, NotFound, DomainError>> AddTeamAsync(TeamData newTeam);
    public ValueTask<OneOf<Success, NotFound, DomainError>> UpdateTeamAsync(int sessionId, int teamId, TeamData teamData, bool tracking);
    public ValueTask<OneOf<Success, NotFound, DomainError>> DeleteTeamAsync(int sessionId, int teamId, bool tracking);

    public sealed record TeamData(
        int SessionId,
        string TeamName,
        TeamRole Role,
        string ColorCode,
        bool IsCaught = false
    );
}

internal sealed class TeamService(IUnitOfWork uow, ILogger<TeamService> logger) : ITeamService
{
    public async ValueTask<IReadOnlyCollection<Team>> GetTeamsBySessionIdAsync(int sessionId, bool tracking)
    {
        IReadOnlyCollection<Team> teams = await uow.TeamRepository.GetTeamsBySessionIdAsync(sessionId, tracking);

        return teams;
    }

    public async ValueTask<OneOf<Team, NotFound>> GetTeamByIdAsync(int sessionId, int teamId, bool tracking)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking);

        if (team is null || team.SessionId != sessionId)
        {
            return new NotFound();
        }

        return team;
    }

    public async ValueTask<OneOf<Team, NotFound, DomainError>> AddTeamAsync(ITeamService.TeamData newTeam)
    {
        try
        {
            var session = await uow.GameSessionRepository.GetSessionByIdAsync(newTeam.SessionId, tracking: false);
            if (session is null)
            {
                logger.LogWarning("Rejected team creation for missing session {SessionId}", newTeam.SessionId);
                return new NotFound();
            }

            if (session.Status != SessionStatus.Waiting)
            {
                logger.LogWarning("Rejected team creation in session {SessionId} because status is {Status}", newTeam.SessionId, session.Status);
                return DomainError.SessionNotJoinable(newTeam.SessionId, session.Status);
            }

            if (newTeam.Role == TeamRole.MrX)
            {
                var existingMrXTeam = await uow.TeamRepository.GetTeamBySessionAndRoleAsync(newTeam.SessionId, TeamRole.MrX, tracking: false);
                if (existingMrXTeam is not null)
                {
                    logger.LogWarning("Rejected team creation in session {SessionId} because an Mr.X team already exists", newTeam.SessionId);
                    return DomainError.MrXTeamAlreadyExists(newTeam.SessionId);
                }
            }

            var team = uow.TeamRepository.AddTeam(
                newTeam.SessionId,
                newTeam.TeamName,
                newTeam.Role,
                newTeam.ColorCode
            );

            team.IsCaught = newTeam.IsCaught;

            await uow.SaveChangesAsync();

            logger.LogInformation("Created team {TeamId} in session {SessionId}", team.Id, newTeam.SessionId);

            return team;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add team {TeamName} in session {SessionId}", newTeam.TeamName, newTeam.SessionId);
            throw;
        }
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> UpdateTeamAsync(int sessionId, int teamId, ITeamService.TeamData teamData, bool tracking)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking);

        if (team is null || team.SessionId != sessionId || teamData.SessionId != sessionId)
        {
            return new NotFound();
        }

        var session = await uow.GameSessionRepository.GetSessionByIdAsync(sessionId, tracking: false);
        if (session is null)
        {
            logger.LogWarning("Rejected update for team {TeamId} because session {SessionId} does not exist", teamId, sessionId);
            return new NotFound();
        }

        if (session.Status != SessionStatus.Waiting)
        {
            logger.LogWarning("Rejected update for team {TeamId} because session {SessionId} is in status {Status}", teamId, sessionId, session.Status);
            return DomainError.SessionNotJoinable(sessionId, session.Status);
        }

        if (teamData.Role == TeamRole.MrX)
        {
            var existingMrXTeam = await uow.TeamRepository.GetTeamBySessionAndRoleAsync(sessionId, TeamRole.MrX, tracking: false);
            if (existingMrXTeam is not null && existingMrXTeam.Id != teamId)
            {
                logger.LogWarning("Rejected update for team {TeamId} because session {SessionId} already has another Mr.X team {ExistingTeamId}", teamId, sessionId, existingMrXTeam.Id);
                return DomainError.MrXTeamAlreadyExists(sessionId);
            }
        }

        team.TeamName = teamData.TeamName;
        team.Role = teamData.Role;
        team.ColorCode = teamData.ColorCode;
        team.IsCaught = teamData.IsCaught;

        await uow.SaveChangesAsync();

        logger.LogInformation("Updated team {TeamId} in session {SessionId}", teamId, sessionId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, DomainError>> DeleteTeamAsync(int sessionId, int teamId, bool tracking)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking);

        if (team is null || team.SessionId != sessionId)
        {
            return new NotFound();
        }

        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionAndTeamIdAsync(sessionId, teamId, tracking: false);
        if (members.Count > 0)
        {
            logger.LogWarning("Rejected delete for team {TeamId} in session {SessionId} because it still has {MemberCount} members", teamId, sessionId, members.Count);
            return DomainError.TeamHasMembers(teamId);
        }

        uow.TeamRepository.RemoveTeam(team);
        await uow.SaveChangesAsync();

        logger.LogInformation("Deleted team {TeamId} from session {SessionId}", teamId, sessionId);

        return new Success();
    }
}
