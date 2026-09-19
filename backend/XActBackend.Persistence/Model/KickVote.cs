namespace XActBackend.Persistence.Model;

public sealed class KickVote
{
    public const int MaxReasonLength = 200;

    /// <summary>how long a vote stays open. after that it gets marked expired on the next start or ballot</summary>
    public const int VoteDurationSeconds = 60;

    public int Id { get; set; }

    public int SessionId { get; set; }

    /// <summary>
    ///     nullable so the vote stays as history after the target is removed, the reference is set to null
    ///     when the member is deleted
    /// </summary>
    public int? TargetMemberId { get; set; }

    /// <summary>nullable for the same reason as <see cref="TargetMemberId"/></summary>
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
