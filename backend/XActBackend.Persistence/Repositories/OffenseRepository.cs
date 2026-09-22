using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IOffenseRepository
{
    public Offense AddOffense(int sessionId, int memberId, OffenseType type, OffenseStatus status, Instant detectedAt);

    public ValueTask<Offense?> GetActiveOffenseAsync(int memberId, OffenseType type, bool tracking);

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
