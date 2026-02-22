using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface ITeamRepository
{
    public Team AddTeam(int sessionId, string teamName, TeamRole role, string colorCode);
    public ValueTask<IReadOnlyCollection<Team>> GetTeamsBySessionIdAsync(int sessionId, bool tracking);
    public ValueTask<Team?> GetTeamByIdAsync(int id, bool tracking);
    public void RemoveTeam(Team team);
}

internal sealed class TeamRepository(DbSet<Team> teamSet) : ITeamRepository
{
    private IQueryable<Team> Teams => teamSet;
    private IQueryable<Team> TeamsNoTracking => Teams.AsNoTracking();

    public Team AddTeam(int sessionId, string teamName, TeamRole role, string colorCode)
    {
        var team = new Team
        {
            SessionId = sessionId,
            TeamName = teamName,
            Role = role,
            ColorCode = colorCode,
        };

        teamSet.Add(team);

        return team;
    }

    public async ValueTask<IReadOnlyCollection<Team>> GetTeamsBySessionIdAsync(int sessionId, bool tracking)
    {
        IQueryable<Team> source = tracking ? Teams : TeamsNoTracking;

        List<Team> teams = await source
            .Where(t => t.SessionId == sessionId)
            .ToListAsync();

        return teams;
    }

    public async ValueTask<Team?> GetTeamByIdAsync(int id, bool tracking)
    {
        IQueryable<Team> source = tracking ? Teams : TeamsNoTracking;

        return await source.FirstOrDefaultAsync(t => t.Id == id);
    }

    public void RemoveTeam(Team team)
    {
        teamSet.Remove(team);
    }
}
