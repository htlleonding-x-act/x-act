namespace XActBackend.Persistence.Model;

public sealed class UserAuthIdentity
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    /// <summary>the keycloak <c>sub</c> claim, a uuid that identifies the user inside the realm</summary>
    public required string ProviderSubject { get; set; }

    public Instant CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
