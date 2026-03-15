using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface ITeamMemberService
{
    public ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int sessionId, int teamId, bool tracking);
    public ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int sessionId, int teamId, int memberId, bool tracking);
    public ValueTask<OneOf<TeamMember, Error>> AddTeamMemberAsync(TeamMemberData newTeamMember);
    public ValueTask<OneOf<Success, NotFound>> UpdateTeamMemberAsync(int sessionId, int teamId, int memberId, TeamMemberData teamMemberData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int sessionId, int teamId, int memberId, bool tracking);

    public sealed record TeamMemberData(
        int SessionId,
        int TeamId,
        int? UserId,
        string? GuestName,
        bool IsTeamLeader = false,
        double? CurrentLatitude = null,
        double? CurrentLongitude = null,
        Instant? LastUpdated = null
    );
}

internal sealed class TeamMemberService(IUnitOfWork uow, IClock clock, ILogger<TeamMemberService> logger) : ITeamMemberService
{
    public async ValueTask<IReadOnlyCollection<TeamMember>> GetMembersByTeamIdAsync(int sessionId, int teamId, bool tracking)
    {
        IReadOnlyCollection<TeamMember> members = await uow.TeamMemberRepository.GetMembersBySessionAndTeamIdAsync(sessionId, teamId, tracking);

        return members;
    }

    public async ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking);

        return member is not null ? member : new NotFound();
    }

    public async ValueTask<OneOf<TeamMember, Error>> AddTeamMemberAsync(ITeamMemberService.TeamMemberData newTeamMember)
    {
        try
        {
            if (newTeamMember.UserId is null && string.IsNullOrWhiteSpace(newTeamMember.GuestName))
            {
                return new Error();
            }

            if (newTeamMember.UserId is not null)
            {
                var existingMember = await uow.TeamMemberRepository.GetMemberBySessionAndUserIdAsync(
                    newTeamMember.SessionId,
                    newTeamMember.UserId.Value,
                    tracking: false
                );

                if (existingMember is not null)
                {
                    return new Error();
                }
            }

            var member = uow.TeamMemberRepository.AddTeamMember(
                newTeamMember.SessionId,
                newTeamMember.TeamId,
                newTeamMember.UserId,
                newTeamMember.GuestName,
                newTeamMember.IsTeamLeader
            );

            member.CurrentLatitude = newTeamMember.CurrentLatitude;
            member.CurrentLongitude = newTeamMember.CurrentLongitude;
            member.LastUpdated = newTeamMember.LastUpdated ?? clock.GetCurrentInstant();

            await uow.SaveChangesAsync();

            return member;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add team member in session {SessionId}, team {TeamId}", newTeamMember.SessionId, newTeamMember.TeamId);
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateTeamMemberAsync(int sessionId, int teamId, int memberId, ITeamMemberService.TeamMemberData teamMemberData, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking);

        if (member is null || teamMemberData.SessionId != sessionId || teamMemberData.TeamId != teamId)
        {
            return new NotFound();
        }

        member.TeamId = teamMemberData.TeamId;
        member.SessionId = teamMemberData.SessionId;
        member.UserId = teamMemberData.UserId;
        member.GuestName = teamMemberData.GuestName;
        member.IsTeamLeader = teamMemberData.IsTeamLeader;
        member.CurrentLatitude = teamMemberData.CurrentLatitude;
        member.CurrentLongitude = teamMemberData.CurrentLongitude;
        member.LastUpdated = teamMemberData.LastUpdated ?? clock.GetCurrentInstant();

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking);

        if (member is null)
        {
            return new NotFound();
        }

        uow.TeamMemberRepository.RemoveTeamMember(member);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
