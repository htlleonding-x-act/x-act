namespace XActBackend.Persistence.Model;

public class UserAuthIdentity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public AuthProvider Provider { get; set; }

    public required string ProviderSubject { get; set; }

    public string? PasswordHash { get; set; }

    public Instant CreatedAt { get; set; }


    public User User { get; set; } = null!;
}
