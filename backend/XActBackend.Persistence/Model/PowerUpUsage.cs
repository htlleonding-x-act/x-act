namespace XActBackend.Persistence.Model;

public class PowerUpUsage
{
    public int Id { get; set; }

    public int MemberId { get; set; }

    public PowerUpType PowerUpType { get; set; }

    public Instant UsedAt { get; set; }


    public TeamMember Member { get; set; } = null!;
}
