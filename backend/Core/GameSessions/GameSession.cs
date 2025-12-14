namespace XAct.Core.GameSessions;

public enum SessionStatus
{
    WAITING,
    ACTIVE,
    FINISHED
}

public class GameSession
{
    public Guid SessionId { get; init; }
    public Guid HostUserId { get; set; }
    public required string JoinCode { get; set; }
    public SessionStatus Status { get; set; }
    public Instant? StartTime { get; set; }
    public Instant? EndTime { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int MrXRevealInterval { get; set; }
}
