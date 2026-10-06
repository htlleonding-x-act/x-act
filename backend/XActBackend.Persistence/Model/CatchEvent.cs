namespace XActBackend.Persistence.Model;

/// <summary>
///     one mr.x catch. a catch swaps the roles of both teams, so this is the only record of who was mr.x when
/// </summary>
public sealed class CatchEvent
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public Instant OccurredAt { get; set; }

    /// <summary>the team that made the catch and became mr.x</summary>
    public int CatchingTeamId { get; set; }

    /// <summary>the team that was mr.x until this catch</summary>
    public int CaughtTeamId { get; set; }


    public GameSession Session { get; set; } = null!;

    public Team CatchingTeam { get; set; } = null!;

    public Team CaughtTeam { get; set; } = null!;
}
