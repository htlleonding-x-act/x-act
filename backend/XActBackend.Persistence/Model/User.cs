namespace XActBackend.Persistence.Model;

public class User
{
    public int Id { get; set; }

    public required string Username { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public AccountType AccountType { get; set; }

    public Instant? SubscriptionEndDate { get; set; }

    public int TotalWins { get; set; }

    public int TotalGamesPlayed { get; set; }


    public ICollection<GameSession> HostedSessions { get; set; } = [];

    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
}
