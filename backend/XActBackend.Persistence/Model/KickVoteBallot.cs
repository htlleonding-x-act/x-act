namespace XActBackend.Persistence.Model;

public sealed class KickVoteBallot
{
    public int Id { get; set; }

    public int KickVoteId { get; set; }

    /// <summary>
    ///     nullable so a cast ballot survives the voter leaving, the reference is set to null when the member
    ///     is deleted
    /// </summary>
    public int? VoterMemberId { get; set; }

    public bool Approve { get; set; }

    public Instant CastAt { get; set; }


    public KickVote KickVote { get; set; } = null!;

    public TeamMember? Voter { get; set; }
}
