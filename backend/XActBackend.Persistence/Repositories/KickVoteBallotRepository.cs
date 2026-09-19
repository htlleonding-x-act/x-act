using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IKickVoteBallotRepository
{
    public KickVoteBallot AddBallot(int kickVoteId, int? voterMemberId, bool approve, Instant castAt);

    public ValueTask<IReadOnlyCollection<KickVoteBallot>> GetBallotsByVoteIdAsync(int kickVoteId, bool tracking);
}

internal sealed class KickVoteBallotRepository(DbSet<KickVoteBallot> ballotSet) : IKickVoteBallotRepository
{
    private IQueryable<KickVoteBallot> Ballots => ballotSet;
    private IQueryable<KickVoteBallot> BallotsNoTracking => Ballots.AsNoTracking();

    public KickVoteBallot AddBallot(int kickVoteId, int? voterMemberId, bool approve, Instant castAt)
    {
        var ballot = new KickVoteBallot
        {
            KickVoteId = kickVoteId,
            VoterMemberId = voterMemberId,
            Approve = approve,
            CastAt = castAt,
        };

        ballotSet.Add(ballot);

        return ballot;
    }

    public async ValueTask<IReadOnlyCollection<KickVoteBallot>> GetBallotsByVoteIdAsync(int kickVoteId, bool tracking)
    {
        IQueryable<KickVoteBallot> source = tracking ? Ballots : BallotsNoTracking;

        List<KickVoteBallot> ballots = await source
            .Where(b => b.KickVoteId == kickVoteId)
            .ToListAsync();

        return ballots;
    }
}
