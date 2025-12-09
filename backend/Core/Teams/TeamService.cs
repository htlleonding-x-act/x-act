using OneOf;
using OneOf.Types;

namespace XAct.Core.Teams;

public interface ITeamService
{
    public ValueTask<IReadOnlyCollection<Team>> GetAllTeamsAsync();
    public ValueTask<OneOf<Team, NotFound>> GetTeamByIdAsync(Guid teamId);
    public ValueTask<OneOf<Team, Error>> AddTeamAsync(TeamData newTeam);
    public ValueTask<OneOf<Success, NotFound>> UpdateTeamAsync(Guid teamId, TeamData teamData);
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamAsync(Guid teamId);

    public sealed record TeamData(
        Guid SessionId,
        string TeamName,
        TeamRole Role,
        string ColorCode,
        bool IsCaught = false
    );
}

public sealed class TeamService(IDataStorage dataStorage) : ITeamService
{
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<Team>> GetAllTeamsAsync()
    {
        IEnumerable<Team> teams = await _dataStorage.GetTeamsAsync();

        return [.. teams];
    }

    public async ValueTask<OneOf<Team, NotFound>> GetTeamByIdAsync(Guid teamId)
    {
        var team = await GetTeamById(teamId);

        return team is not null ? team : new NotFound();
    }

    public async ValueTask<OneOf<Team, Error>> AddTeamAsync(ITeamService.TeamData newTeam)
    {
        try
        {
            var team = new Team
            {
                TeamId = Guid.NewGuid(),
                SessionId = newTeam.SessionId,
                TeamName = newTeam.TeamName,
                Role = newTeam.Role,
                ColorCode = newTeam.ColorCode,
                IsCaught = newTeam.IsCaught
            };

            await _dataStorage.AddTeamAsync(team);

            return team;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateTeamAsync(Guid teamId, ITeamService.TeamData teamData)
    {
        var team = await GetTeamById(teamId);

        if (team is null)
        {
            return new NotFound();
        }

        team.SessionId = teamData.SessionId;
        team.TeamName = teamData.TeamName;
        team.Role = teamData.Role;
        team.ColorCode = teamData.ColorCode;
        team.IsCaught = teamData.IsCaught;

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteTeamAsync(Guid teamId)
    {
        var team = await GetTeamById(teamId);

        if (team is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemoveTeamAsync(team);

        return new Success();
    }

    private async ValueTask<Team?> GetTeamById(Guid teamId)
    {
        IEnumerable<Team> teams = await _dataStorage.GetTeamsAsync();

        return teams.FirstOrDefault(t => t.TeamId == teamId);
    }
}
