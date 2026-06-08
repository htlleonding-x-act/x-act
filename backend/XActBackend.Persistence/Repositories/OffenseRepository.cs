using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="Offense"/> entities.
/// </summary>
public interface IOffenseRepository
{
    /// <summary>
    ///     Add a new offense.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="memberId">The offending member</param>
    /// <param name="type">The kind of rule that was broken</param>
    /// <param name="status">The initial status of the offense</param>
    /// <param name="detectedAt">Timestamp the offense was detected</param>
    /// <returns>The created tracked offense entity</returns>
    public Offense AddOffense(int sessionId, int memberId, OffenseType type, OffenseStatus status, Instant detectedAt);

    /// <summary>
    ///     Get the active offense of a member for a given type, if any.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="type">The kind of offense</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The active offense, or <c>null</c> if the member currently has none of this type</returns>
    public ValueTask<Offense?> GetActiveOffenseAsync(int memberId, OffenseType type, bool tracking);

    /// <summary>
    ///     Get all active offenses for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All active offenses for the session</returns>
    public ValueTask<IReadOnlyCollection<Offense>> GetActiveOffensesBySessionAsync(int sessionId, bool tracking);
}

internal sealed class OffenseRepository(DbSet<Offense> offenseSet) : IOffenseRepository
{
    private IQueryable<Offense> Offenses => offenseSet;
    private IQueryable<Offense> OffensesNoTracking => Offenses.AsNoTracking();

    public Offense AddOffense(int sessionId, int memberId, OffenseType type, OffenseStatus status, Instant detectedAt)
    {
        var offense = new Offense
        {
            SessionId = sessionId,
            MemberId = memberId,
            Type = type,
            Status = status,
            DetectedAt = detectedAt,
        };

        offenseSet.Add(offense);

        return offense;
    }

    public async ValueTask<Offense?> GetActiveOffenseAsync(int memberId, OffenseType type, bool tracking)
    {
        IQueryable<Offense> source = tracking ? Offenses : OffensesNoTracking;

        return await source
            .Where(o => o.MemberId == memberId && o.Type == type && o.Status == OffenseStatus.Active)
            .OrderByDescending(o => o.Id)
            .FirstOrDefaultAsync();
    }

    public async ValueTask<IReadOnlyCollection<Offense>> GetActiveOffensesBySessionAsync(int sessionId, bool tracking)
    {
        IQueryable<Offense> source = tracking ? Offenses : OffensesNoTracking;

        List<Offense> offenses = await source
            .Where(o => o.SessionId == sessionId && o.Status == OffenseStatus.Active)
            .OrderBy(o => o.DetectedAt)
            .ToListAsync();

        return offenses;
    }
}
