namespace XActBackend.Persistence.Model;

public sealed class GameSession
{
    public int Id { get; set; }

    public required string HostUserId { get; set; }

    public required string SessionName { get; set; }

    public required string JoinCode { get; set; }

    public SessionStatus Status { get; set; }

    public Instant? StartTime { get; set; }

    public Instant? EndTime { get; set; }

    /// <summary>null while the session runs, and for sessions finished before the reason was stored</summary>
    public GameEndReason? EndReason { get; set; }

    public int PlannedDurationMinutes { get; set; }

    /// <summary>minutes between two mr.x reveals</summary>
    public int MrXRevealInterval { get; set; }

    public Instant CreatedAt { get; set; }


    public User Host { get; set; } = null!;

    public ICollection<Team> Teams { get; set; } = [];

    public ICollection<GeofencePoint> GeofencePoints { get; set; } = [];
}
