using OneOf;
using OneOf.Types;

namespace XAct.Core.TeamMembers;

public interface ITeamMemberService
{
    public ValueTask<IReadOnlyCollection<TeamMember>> GetAllTeamMembersAsync();
    public ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int memberId);
    public ValueTask<OneOf<TeamMember, Error>> AddTeamMemberAsync(TeamMemberData newTeamMember);
    public ValueTask<OneOf<Success, NotFound>> UpdateTeamMemberAsync(int memberId, TeamMemberData teamMemberData);
    public ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int memberId);

    public sealed record TeamMemberData(
        int TeamId,
        int UserId,
        bool IsTeamLeader = false,
        double? CurrentLatitude = null,
        double? CurrentLongitude = null,
        Instant? LastUpdated = null
    );
}

public sealed class TeamMemberService(IDataStorage dataStorage, IClock clock) : ITeamMemberService
{
    private static int _nextMemberId = 6;
    private readonly IDataStorage _dataStorage = dataStorage;
    private readonly IClock _clock = clock;

    public async ValueTask<IReadOnlyCollection<TeamMember>> GetAllTeamMembersAsync()
    {
        IEnumerable<TeamMember> teamMembers = await _dataStorage.GetTeamMembersAsync();

        return [.. teamMembers];
    }

    public async ValueTask<OneOf<TeamMember, NotFound>> GetTeamMemberByIdAsync(int memberId)
    {
        var teamMember = await GetTeamMemberById(memberId);

        return teamMember is not null ? teamMember : new NotFound();
    }

    public async ValueTask<OneOf<TeamMember, Error>> AddTeamMemberAsync(ITeamMemberService.TeamMemberData newTeamMember)
    {
        try
        {
            var teamMember = new TeamMember
            {
                MemberId = _nextMemberId++,
                TeamId = newTeamMember.TeamId,
                UserId = newTeamMember.UserId,
                IsTeamLeader = newTeamMember.IsTeamLeader,
                CurrentLatitude = newTeamMember.CurrentLatitude,
                CurrentLongitude = newTeamMember.CurrentLongitude,
                LastUpdated = newTeamMember.LastUpdated ?? _clock.GetCurrentInstant()
            };

            await _dataStorage.AddTeamMemberAsync(teamMember);

            return teamMember;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdateTeamMemberAsync(int memberId, ITeamMemberService.TeamMemberData teamMemberData)
    {
        var teamMember = await GetTeamMemberById(memberId);

        if (teamMember is null)
        {
            return new NotFound();
        }

        teamMember.TeamId = teamMemberData.TeamId;
        teamMember.UserId = teamMemberData.UserId;
        teamMember.IsTeamLeader = teamMemberData.IsTeamLeader;
        teamMember.CurrentLatitude = teamMemberData.CurrentLatitude;
        teamMember.CurrentLongitude = teamMemberData.CurrentLongitude;
        teamMember.LastUpdated = teamMemberData.LastUpdated ?? _clock.GetCurrentInstant();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteTeamMemberAsync(int memberId)
    {
        var teamMember = await GetTeamMemberById(memberId);

        if (teamMember is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemoveTeamMemberAsync(teamMember);

        return new Success();
    }

    private async ValueTask<TeamMember?> GetTeamMemberById(int memberId)
    {
        IEnumerable<TeamMember> teamMembers = await _dataStorage.GetTeamMembersAsync();

        return teamMembers.FirstOrDefault(tm => tm.MemberId == memberId);
    }
}
