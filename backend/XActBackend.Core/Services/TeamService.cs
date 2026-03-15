using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ITeamService
{
    public ValueTask<IReadOnlyCollection<Team>> GetTeamsBySessionIdAsync(int sessionId, bool tracking);
    public ValueTask<OneOf<Team, NotFound>> GetTeamByIdAsync(int sessionId, int teamId, bool tracking);
    public ValueTask<OneOf<Team, Error>> AddTeamAsync(TeamData newTeam);
    public ValueTask<OneOf<Success, NotFound>> UpdateTeamAsync(int sessionId, int teamId, TeamData teamData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamAsync(int sessionId, int teamId, bool tracking);

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

    public async ValueTask<OneOf<Team, Error>> AddTeamAsync(ITeamService.TeamData newTeam)
    {
        try
        {
            var team = uow.TeamRepository.AddTeam(
                newTeam.SessionId,
                newTeam.TeamName,
                newTeam.Role,
                newTeam.ColorCode
            );

            team.IsCaught = newTeam.IsCaught;

            await uow.SaveChangesAsync();

            return team;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add team {TeamName} in session {SessionId}", newTeam.TeamName, newTeam.SessionId);
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateTeamAsync(int sessionId, int teamId, ITeamService.TeamData teamData, bool tracking)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking);

        if (team is null || team.SessionId != sessionId || teamData.SessionId != sessionId)
        {
            return new NotFound();
        }

        team.TeamName = teamData.TeamName;
        team.Role = teamData.Role;
        team.ColorCode = teamData.ColorCode;
        team.IsCaught = teamData.IsCaught;

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteTeamAsync(int sessionId, int teamId, bool tracking)
    {
        var team = await uow.TeamRepository.GetTeamByIdAsync(teamId, tracking);

        if (team is null || team.SessionId != sessionId)
        {
            return new NotFound();
        }

        uow.TeamRepository.RemoveTeam(team);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
