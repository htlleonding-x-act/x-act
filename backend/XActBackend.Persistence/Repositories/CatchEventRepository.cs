using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface ICatchEventRepository
{
    public CatchEvent AddCatchEvent(int sessionId, int catchingTeamId, int caughtTeamId, Instant occurredAt);

    public ValueTask<IReadOnlyCollection<CatchEvent>> GetCatchEventsBySessionIdAsync(int sessionId, bool tracking);
}

internal sealed class CatchEventRepository(DbSet<CatchEvent> catchEventSet) : ICatchEventRepository
{
    private IQueryable<CatchEvent> CatchEvents => catchEventSet;
    private IQueryable<CatchEvent> CatchEventsNoTracking => CatchEvents.AsNoTracking();

    public CatchEvent AddCatchEvent(int sessionId, int catchingTeamId, int caughtTeamId, Instant occurredAt)
    {
        var catchEvent = new CatchEvent
        {
            SessionId = sessionId,
            CatchingTeamId = catchingTeamId,
            CaughtTeamId = caughtTeamId,
            OccurredAt = occurredAt,
        };

        catchEventSet.Add(catchEvent);

        return catchEvent;
    }

    public async ValueTask<IReadOnlyCollection<CatchEvent>> GetCatchEventsBySessionIdAsync(int sessionId, bool tracking)
    {
        IQueryable<CatchEvent> source = tracking ? CatchEvents : CatchEventsNoTracking;

        List<CatchEvent> catchEvents = await source
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.OccurredAt)
            .ThenBy(c => c.Id)
            .ToListAsync();

        return catchEvents;
    }
}
