using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="KickVote"/> entities.
/// </summary>
public interface IKickVoteRepository
{
    /// <summary>
    ///     Add a new open kick vote.
    /// </summary>
    /// <param name="sessionId">The id of the session the vote belongs to</param>
    /// <param name="targetMemberId">The member the vote wants to kick</param>
    /// <param name="initiatorMemberId">The member that started the vote</param>
    /// <param name="reason">Optional reason for the kick</param>
    /// <param name="createdAt">Timestamp the vote was started</param>
    /// <param name="expiresAt">Timestamp the voting window closes</param>
    /// <returns>The created tracked kick vote entity</returns>
    public KickVote AddKickVote(
        int sessionId,
        int? targetMemberId,
        int? initiatorMemberId,
        string? reason,
        Instant createdAt,
        Instant expiresAt
    );

    /// <summary>
    ///     Get a kick vote by id.
    /// </summary>
    /// <param name="voteId">The id of the kick vote</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The kick vote, if found</returns>
    public ValueTask<KickVote?> GetByIdAsync(int voteId, bool tracking);

    /// <summary>
    ///     Get the single open kick vote of a session, if any.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The open kick vote, or <c>null</c> if none is open</returns>
    public ValueTask<KickVote?> GetOpenVoteBySessionAsync(int sessionId, bool tracking);
}

internal sealed class KickVoteRepository(DbSet<KickVote> voteSet) : IKickVoteRepository
{
    private IQueryable<KickVote> Votes => voteSet;
    private IQueryable<KickVote> VotesNoTracking => Votes.AsNoTracking();

    public KickVote AddKickVote(
        int sessionId,
        int? targetMemberId,
        int? initiatorMemberId,
        string? reason,
        Instant createdAt,
        Instant expiresAt
    )
    {
        var vote = new KickVote
        {
            SessionId = sessionId,
            TargetMemberId = targetMemberId,
            InitiatorMemberId = initiatorMemberId,
            Reason = reason,
            Status = KickVoteStatus.Open,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
        };

        voteSet.Add(vote);

        return vote;
    }

    public async ValueTask<KickVote?> GetByIdAsync(int voteId, bool tracking)
    {
        IQueryable<KickVote> source = tracking ? Votes : VotesNoTracking;

        return await source.FirstOrDefaultAsync(v => v.Id == voteId);
    }

    public async ValueTask<KickVote?> GetOpenVoteBySessionAsync(int sessionId, bool tracking)
    {
        IQueryable<KickVote> source = tracking ? Votes : VotesNoTracking;

        return await source
            .Where(v => v.SessionId == sessionId && v.Status == KickVoteStatus.Open)
            .OrderByDescending(v => v.Id)
            .FirstOrDefaultAsync();
    }
}
