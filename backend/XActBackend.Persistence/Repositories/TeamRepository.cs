using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="Team"/> entities.
/// </summary>
public interface ITeamRepository
{
    /// <summary>
    ///     Add a new team.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamName">The name of the team</param>
    /// <param name="role">The role of the team</param>
    /// <param name="colorCode">The color code of the team</param>
    /// <returns>The created team entity</returns>
    public Team AddTeam(int sessionId, string teamName, TeamRole role, string colorCode);

    /// <summary>
    ///     Get all teams of a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All teams of the session</returns>
    public ValueTask<IReadOnlyCollection<Team>> GetTeamsBySessionIdAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Get a team by id.
    /// </summary>
    /// <param name="id">The id of the team</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The team, if found</returns>
    public ValueTask<Team?> GetTeamByIdAsync(int id, bool tracking);

    /// <summary>
    ///     Get a team by session id and role.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="role">The role of the team to search for</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The team, if found</returns>
    public ValueTask<Team?> GetTeamBySessionAndRoleAsync(int sessionId, TeamRole role, bool tracking);

    /// <summary>
    ///     Remove a team from the repository.
    /// </summary>
    /// <param name="team">The team to remove</param>
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

    public async ValueTask<Team?> GetTeamBySessionAndRoleAsync(int sessionId, TeamRole role, bool tracking)
    {
        IQueryable<Team> source = tracking ? Teams : TeamsNoTracking;

        return await source.FirstOrDefaultAsync(t => t.SessionId == sessionId && t.Role == role);
    }

    public void RemoveTeam(Team team)
    {
        teamSet.Remove(team);
    }
}
