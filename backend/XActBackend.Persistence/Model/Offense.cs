namespace XActBackend.Persistence.Model;

/// <summary>
///     a rule violation the server detects on its own, like leaving the game area. active offenses make up
///     the flagged players list in the report tab
/// </summary>
public sealed class Offense
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public int MemberId { get; set; }

    public OffenseType Type { get; set; }

    public OffenseStatus Status { get; set; }

    public Instant DetectedAt { get; set; }

    public Instant? ClearedAt { get; set; }


    public GameSession Session { get; set; } = null!;

    public TeamMember Member { get; set; } = null!;
}
