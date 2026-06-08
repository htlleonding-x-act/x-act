using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="KickVoteBallot"/> entities.
/// </summary>
public interface IKickVoteBallotRepository
{
    /// <summary>
    ///     Add a new ballot to a kick vote.
    /// </summary>
    /// <param name="kickVoteId">The id of the kick vote</param>
    /// <param name="voterMemberId">The member casting the ballot</param>
    /// <param name="approve"><c>true</c> approves the kick, <c>false</c> votes to keep the target</param>
    /// <param name="castAt">Timestamp the ballot was cast</param>
    /// <returns>The created tracked ballot entity</returns>
    public KickVoteBallot AddBallot(int kickVoteId, int? voterMemberId, bool approve, Instant castAt);

    /// <summary>
    ///     Get all ballots cast for a kick vote.
    /// </summary>
    /// <param name="kickVoteId">The id of the kick vote</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All ballots for the vote</returns>
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
