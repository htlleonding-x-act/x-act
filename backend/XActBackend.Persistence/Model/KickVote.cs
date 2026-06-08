namespace XActBackend.Persistence.Model;

public class KickVote
{
    public const int MaxReasonLength = 200;

    /// <summary>How long a vote stays open before it can be lazily expired.</summary>
    public const int VoteDurationSeconds = 60;

    public int Id { get; set; }

    public int SessionId { get; set; }

    /// <summary>
    ///     The member the vote wants to kick. Nullable so the vote survives as history once the
    ///     target has been removed from the session (the reference is set to null on member deletion).
    /// </summary>
    public int? TargetMemberId { get; set; }

    /// <summary>
    ///     The member that started the vote. Nullable for the same history-preserving reason as
    ///     <see cref="TargetMemberId"/>.
    /// </summary>
    public int? InitiatorMemberId { get; set; }

    public string? Reason { get; set; }

    public KickVoteStatus Status { get; set; }

    public Instant CreatedAt { get; set; }

    public Instant ExpiresAt { get; set; }

    public Instant? ResolvedAt { get; set; }


    public GameSession Session { get; set; } = null!;

    public TeamMember? TargetMember { get; set; }

    public TeamMember? InitiatorMember { get; set; }

    public ICollection<KickVoteBallot> Ballots { get; set; } = [];
}
