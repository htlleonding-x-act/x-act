namespace XAct.Core.TeamMembers;

public class TeamMember
{
    public Guid MemberId { get; init; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public bool IsTeamLeader { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public Instant? LastUpdated { get; set; }
}
