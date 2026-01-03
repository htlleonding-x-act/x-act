namespace XAct.Core.TeamMembers;

public class TeamMember
{
    public int MemberId { get; init; }
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public bool IsTeamLeader { get; set; }
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public Instant? LastUpdated { get; set; }
}
