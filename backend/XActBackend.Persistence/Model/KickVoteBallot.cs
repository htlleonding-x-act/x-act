namespace XActBackend.Persistence.Model;

public class KickVoteBallot
{
    public int Id { get; set; }

    public int KickVoteId { get; set; }

    /// <summary>
    ///     The member that cast this ballot. Nullable so a cast ballot survives the voter leaving the
    ///     session (the reference is set to null on member deletion).
    /// </summary>
    public int? VoterMemberId { get; set; }

    /// <summary><c>true</c> approves the kick, <c>false</c> votes to keep the target.</summary>
    public bool Approve { get; set; }

    public Instant CastAt { get; set; }


    public KickVote KickVote { get; set; } = null!;

    public TeamMember? Voter { get; set; }
}
