namespace XActBackend.Persistence.Model;

public class User
{
    public string? Id { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public AccountType AccountType { get; set; }

    public Instant? SubscriptionEndDate { get; set; }

    public int TotalWins { get; set; }

    public int TotalGamesPlayed { get; set; }

    public bool IsDeleted { get; set; }

    public Instant? DeletedAt { get; set; }

    public Instant CreatedAt { get; set; }


    public ICollection<GameSession> HostedSessions { get; set; } = [];

    public ICollection<UserAuthIdentity> AuthIdentities { get; set; } = [];

    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
}
