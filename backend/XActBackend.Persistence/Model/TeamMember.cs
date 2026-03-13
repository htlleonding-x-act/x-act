namespace XActBackend.Persistence.Model;

public class TeamMember
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public int TeamId { get; set; }

    public int? UserId { get; set; }

    public string? GuestName { get; set; }

    public bool IsTeamLeader { get; set; }

    public Instant JoinedAt { get; set; }

    public double? CurrentLatitude { get; set; }

    public double? CurrentLongitude { get; set; }

    public Instant? LastUpdated { get; set; }


    public GameSession Session { get; set; } = null!;

    public Team Team { get; set; } = null!;

    public User? User { get; set; }

    public ICollection<LocationLog> LocationLogs { get; set; } = [];

    public ICollection<PowerUpUsage> PowerUpUsages { get; set; } = [];
}
