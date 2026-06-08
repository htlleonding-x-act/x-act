namespace XActBackend.Persistence.Model;

/// <summary>
///     An automatically detected rule violation by a team member (e.g. leaving the game area).
///     Active offenses drive the report tab's "flagged players" list shown to everyone.
/// </summary>
public class Offense
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
