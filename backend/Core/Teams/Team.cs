namespace XAct.Core.Teams;

public enum TeamRole
{
    MR_X,
    DETECTIVE,
    SPECTATOR
}

public class Team
{
    public Guid TeamId { get; init; }
    public Guid SessionId { get; set; }
    public required string TeamName { get; set; }
    public TeamRole Role { get; set; }
    public required string ColorCode { get; set; }
    public bool IsCaught { get; set; }
}
