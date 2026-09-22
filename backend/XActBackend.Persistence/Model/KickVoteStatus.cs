namespace XActBackend.Persistence.Model;

public enum KickVoteStatus
{
    Open = 10,

    /// <summary>enough members approved and the target was removed</summary>
    Passed = 20,

    /// <summary>the vote can no longer reach the approval threshold</summary>
    Rejected = 30,

    /// <summary>the initiator or the host cancelled it before it resolved</summary>
    Cancelled = 40,

    /// <summary>the voting window ran out before the threshold was reached</summary>
    Expired = 50,
}
