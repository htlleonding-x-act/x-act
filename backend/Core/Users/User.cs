namespace XAct.Core.Users;

public enum AccountType
{
    FREE,
    PRO,
    EVENT_PASS
}

public class User
{
    public Guid UserId { get; init; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public AccountType AccountType { get; set; }
    public Instant? SubscriptionEndDate { get; set; }
    public int TotalWins { get; set; }
    public int TotalGamesPlayed { get; set; }
}
