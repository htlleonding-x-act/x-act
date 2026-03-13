using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface ITeamMemberRepository
{
    public TeamMember AddTeamMember(int sessionId, int teamId, int? userId, string? guestName, bool isTeamLeader);
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking);
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersBySessionIdAsync(int sessionId, bool tracking);
    public ValueTask<TeamMember?> GetMemberByIdAsync(int id, bool tracking);
    public ValueTask<TeamMember?> GetMemberBySessionAndUserIdAsync(int sessionId, int userId, bool tracking);
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
