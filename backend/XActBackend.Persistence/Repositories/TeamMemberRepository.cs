using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface ITeamMemberRepository
{
    public TeamMember AddTeamMember(int sessionId, int teamId, string? userId, string? guestName, bool isTeamLeader);

    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking);

    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionAndTeamIdAsync(int sessionId, int teamId, bool tracking);

    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionIdAsync(int sessionId, bool tracking);

    public ValueTask<TeamMember?> GetMemberByIdAsync(int id, bool tracking);

    public ValueTask<TeamMember?> GetMemberBySessionAndTeamIdAsync(int sessionId, int teamId, int memberId, bool tracking);

    public ValueTask<TeamMember?> GetMemberBySessionAndUserIdAsync(int sessionId, string userId, bool tracking);

    /// <summary>whether a member of the session other than <paramref name="excludedMemberId"/> goes by the name, ignoring case</summary>
    public ValueTask<bool> IsNameTakenInSessionAsync(int sessionId, string name, int? excludedMemberId);

    public void RemoveTeamMember(TeamMember member);
}

internal sealed class TeamMemberRepository(DbSet<TeamMember> memberSet, IClock clock) : ITeamMemberRepository
{
    private IQueryable<TeamMember> Members => memberSet;
    private IQueryable<TeamMember> MembersNoTracking => Members.AsNoTracking();

    public TeamMember AddTeamMember(int sessionId, int teamId, string? userId, string? guestName, bool isTeamLeader)
    {
        var member = new TeamMember
        {
            SessionId = sessionId,
            TeamId = teamId,
            UserId = userId,
            GuestName = guestName,
            IsTeamLeader = isTeamLeader,
            JoinedAt = clock.GetCurrentInstant(),
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

    public async ValueTask<TeamMember?> GetMemberBySessionAndUserIdAsync(int sessionId, string userId, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        return await source.FirstOrDefaultAsync(m => m.SessionId == sessionId && m.UserId == userId);
    }

    public async ValueTask<bool> IsNameTakenInSessionAsync(int sessionId, string name, int? excludedMemberId)
    {
        string lowered = name.ToLower();

        // a member goes by the username of its user, or by its guest name when it has no user
        return await MembersNoTracking
            .Where(m => m.SessionId == sessionId && m.Id != excludedMemberId)
            .AnyAsync(m => (m.UserId != null ? m.User!.Username : m.GuestName)!.ToLower() == lowered);
    }

    public void RemoveTeamMember(TeamMember member)
    {
        memberSet.Remove(member);
    }
}
