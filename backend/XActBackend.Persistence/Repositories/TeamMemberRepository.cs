using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="TeamMember"/> entities.
/// </summary>
public interface ITeamMemberRepository
{
    /// <summary>
    ///     Add a new team member.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="userId">Optional user id for registered users</param>
    /// <param name="guestName">Optional guest name for unregistered users</param>
    /// <param name="isTeamLeader">Flag indicating if the member is team leader</param>
    /// <returns>The created tracked team member entity</returns>
    public TeamMember AddTeamMember(int sessionId, int teamId, int? userId, string? guestName, bool isTeamLeader);

    /// <summary>
    ///     Get all members of a team.
    /// </summary>
    /// <param name="teamId">The id of the team</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All team members for the team</returns>
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking);

    /// <summary>
    ///     Get all members of a team in a session by the session id and team id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All members of the team in the session</returns>
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionAndTeamIdAsync(int sessionId, int teamId, bool tracking);

    /// <summary>
    ///     Get all members of a session by the session id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All members of the session</returns>
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionIdAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Get a member by id.
    /// </summary>
    /// <param name="id">The id of the member</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The team member, if found</returns>
    public ValueTask<TeamMember?> GetMemberByIdAsync(int id, bool tracking);

    /// <summary>
    ///     Get a member by session id, team id, and member id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="teamId">The id of the team</param>
    /// <param name="memberId">The id of the member</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The team member, if found</returns>
    public ValueTask<TeamMember?> GetMemberBySessionAndTeamIdAsync(int sessionId, int teamId, int memberId, bool tracking);

    /// <summary>
    ///     Get a member by session id and user id.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="userId">The id of the user</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The team member, if found</returns>
    public ValueTask<TeamMember?> GetMemberBySessionAndUserIdAsync(int sessionId, int userId, bool tracking);

    /// <summary>
    ///     Remove a team member from the repository.
    /// </summary>
    /// <param name="member">The team member to remove</param>
    public void RemoveTeamMember(TeamMember member);
}

internal sealed class TeamMemberRepository(DbSet<TeamMember> memberSet) : ITeamMemberRepository
{
    private IQueryable<TeamMember> Members => memberSet;
    private IQueryable<TeamMember> MembersNoTracking => Members.AsNoTracking();

    public TeamMember AddTeamMember(int sessionId, int teamId, int? userId, string? guestName, bool isTeamLeader)
    {
        var member = new TeamMember
        {
            SessionId = sessionId,
            TeamId = teamId,
            UserId = userId,
            GuestName = guestName,
            IsTeamLeader = isTeamLeader,
            JoinedAt = SystemClock.Instance.GetCurrentInstant(),
        };

        memberSet.Add(member);

        return member;
    }

    public async ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        List<TeamMember> members = await source
            .Where(m => m.TeamId == teamId)
            .ToListAsync();

        return members;
    }

    public async ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionAndTeamIdAsync(int sessionId, int teamId, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        List<TeamMember> members = await source
            .Where(m => m.SessionId == sessionId && m.TeamId == teamId)
            .ToListAsync();

        return members;
    }

    public async ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionIdAsync(int sessionId, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        List<TeamMember> members = await source
            .Where(m => m.SessionId == sessionId)
            .ToListAsync();

        return members;
    }

    public async ValueTask<TeamMember?> GetMemberByIdAsync(int id, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        return await source.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async ValueTask<TeamMember?> GetMemberBySessionAndTeamIdAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        return await source.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.TeamId == teamId && m.Id == memberId);
    }

    public async ValueTask<TeamMember?> GetMemberBySessionAndUserIdAsync(int sessionId, int userId, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        return await source.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId);
    }

    public void RemoveTeamMember(TeamMember member)
    {
        memberSet.Remove(member);
    }
}
