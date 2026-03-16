namespace XActBackend.Persistence.Model;

public class UserAuthIdentity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// The Keycloak <c>sub</c> claim — a UUID that uniquely identifies
    /// this user inside the Keycloak realm.
    /// </summary>
    public required string ProviderSubject { get; set; }


    public Instant CreatedAt { get; set; }


    public User User { get; set; } = null!;
}
