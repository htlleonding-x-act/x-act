namespace XActBackend.Persistence.Model;

public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }

    public int UserId { get; set; }

    public bool IsTeamLeader { get; set; }

    public double? CurrentLatitude { get; set; }

    public double? CurrentLongitude { get; set; }

    public Instant? LastUpdated { get; set; }


    public Team Team { get; set; } = null!;

    public User User { get; set; } = null!;

    public ICollection<LocationLog> LocationLogs { get; set; } = [];

    public ICollection<PowerUpUsage> PowerUpUsages { get; set; } = [];
}
