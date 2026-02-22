using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface ITeamMemberRepository
{
    public TeamMember AddTeamMember(int teamId, int userId, bool isTeamLeader);
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking);
    public ValueTask<TeamMember?> GetMemberByIdAsync(int id, bool tracking);
    public void RemoveTeamMember(TeamMember member);
}

internal sealed class TeamMemberRepository(DbSet<TeamMember> memberSet) : ITeamMemberRepository
{
    private IQueryable<TeamMember> Members => memberSet;
    private IQueryable<TeamMember> MembersNoTracking => Members.AsNoTracking();

    public TeamMember AddTeamMember(int teamId, int userId, bool isTeamLeader)
    {
        var member = new TeamMember
        {
            TeamId = teamId,
            UserId = userId,
            IsTeamLeader = isTeamLeader,
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

    public async ValueTask<TeamMember?> GetMemberByIdAsync(int id, bool tracking)
    {
        IQueryable<TeamMember> source = tracking ? Members : MembersNoTracking;

        return await source.FirstOrDefaultAsync(m => m.Id == id);
    }

    public void RemoveTeamMember(TeamMember member)
    {
        memberSet.Remove(member);
    }
}
