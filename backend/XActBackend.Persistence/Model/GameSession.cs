namespace XActBackend.Persistence.Model;

public class GameSession
{
    public int Id { get; set; }

    public int HostUserId { get; set; }

    public required string JoinCode { get; set; }

    public SessionStatus Status { get; set; }

    public Instant? StartTime { get; set; }

    public Instant? EndTime { get; set; }

    public int PlannedDurationMinutes { get; set; }

    public int MrXRevealInterval { get; set; }


    public User Host { get; set; } = null!;

    public ICollection<Team> Teams { get; set; } = [];

    public ICollection<GeofencePoint> GeofencePoints { get; set; } = [];
}
