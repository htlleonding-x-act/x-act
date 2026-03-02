using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ITeamMemberService
{
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking);
    public ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int memberId, bool tracking);
    public ValueTask<OneOf<TeamMember, Error>> AddTeamMemberAsync(TeamMemberData newTeamMember);
    public ValueTask<OneOf<Success, NotFound>> UpdateTeamMemberAsync(int memberId, TeamMemberData teamMemberData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int memberId, bool tracking);

    public sealed record TeamMemberData(
        int TeamId,
        int UserId,
        bool IsTeamLeader = false,
        double? CurrentLatitude = null,
        double? CurrentLongitude = null,
        Instant? LastUpdated = null
    );
}

internal sealed class TeamMemberService(IUnitOfWork uow, IClock clock) : ITeamMemberService
{
    public async ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int teamId, bool tracking)
    {
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersByTeamIdAsync(teamId, tracking);

        return members;
    }

    public async ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberByIdAsync(memberId, tracking);

        return member is not null ? member : new NotFound();
    }

    public async ValueTask<OneOf<TeamMember, Error>> AddTeamMemberAsync(ITeamMemberService.TeamMemberData newTeamMember)
    {
        try
        {
            var member = uow.TeamMemberRepository.AddTeamMember(
                newTeamMember.TeamId,
                newTeamMember.UserId,
                newTeamMember.IsTeamLeader
            );

            member.CurrentLatitude = newTeamMember.CurrentLatitude;
            member.CurrentLongitude = newTeamMember.CurrentLongitude;
            member.LastUpdated = newTeamMember.LastUpdated ?? clock.GetCurrentInstant();

            await uow.SaveChangesAsync();

            return member;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateTeamMemberAsync(int memberId, ITeamMemberService.TeamMemberData teamMemberData, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberByIdAsync(memberId, tracking);

        if (member is null)
        {
            return new NotFound();
        }

        member.TeamId = teamMemberData.TeamId;
        member.UserId = teamMemberData.UserId;
        member.IsTeamLeader = teamMemberData.IsTeamLeader;
        member.CurrentLatitude = teamMemberData.CurrentLatitude;
        member.CurrentLongitude = teamMemberData.CurrentLongitude;
        member.LastUpdated = teamMemberData.LastUpdated ?? clock.GetCurrentInstant();

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberByIdAsync(memberId, tracking);

        if (member is null)
        {
            return new NotFound();
        }

        uow.TeamMemberRepository.RemoveTeamMember(member);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
