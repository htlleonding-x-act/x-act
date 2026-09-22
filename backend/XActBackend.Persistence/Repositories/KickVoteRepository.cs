using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IKickVoteRepository
{
    public KickVote AddKickVote(
        int sessionId,
        int? targetMemberId,
        int? initiatorMemberId,
        string? reason,
        Instant createdAt,
        Instant expiresAt
    );

    public ValueTask<KickVote?> GetByIdAsync(int voteId, bool tracking);

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
