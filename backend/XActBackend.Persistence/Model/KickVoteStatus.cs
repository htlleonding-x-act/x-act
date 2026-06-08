namespace XActBackend.Persistence.Model;

public enum KickVoteStatus
{
    /// <summary>The vote is running and members can still cast ballots.</summary>
    Open = 10,

    /// <summary>Enough members approved the kick; the target was removed.</summary>
    Passed = 20,

    /// <summary>The vote could no longer reach the approval threshold.</summary>
    Rejected = 30,

    /// <summary>The initiator or the host cancelled the vote before it resolved.</summary>
    Cancelled = 40,

    /// <summary>The voting window elapsed without reaching the approval threshold.</summary>
    Expired = 50,
}
