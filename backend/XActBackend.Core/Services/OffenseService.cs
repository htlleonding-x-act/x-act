using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IOffenseService
{
    public ValueTask<IReadOnlyCollection<Offense>> GetActiveOffensesBySessionAsync(int sessionId, bool tracking);

    /// <summary>
    ///     raises or clears the out-of-bounds offense for the member's latest position. safe to call on every
    ///     location ping
    /// </summary>
    public ValueTask<OffenseEvaluation> EvaluateMemberLocationAsync(int sessionId, int memberId, double latitude, double longitude);

    public enum OffenseChange
    {
        None,
        Raised,
        Cleared,
    }

    /// <param name="Offense">set whenever <paramref name="Change"/> is not <c>None</c></param>
    public sealed record OffenseEvaluation(OffenseChange Change, Offense? Offense);
}

internal sealed class OffenseService(IUnitOfWork uow, IClock clock, ILogger<OffenseService> logger) : IOffenseService
{
    public async ValueTask<IReadOnlyCollection<Offense>> GetActiveOffensesBySessionAsync(int sessionId, bool tracking) =>
        await uow.OffenseRepository.GetActiveOffensesBySessionAsync(sessionId, tracking);

    public async ValueTask<IOffenseService.OffenseEvaluation> EvaluateMemberLocationAsync(int sessionId, int memberId, double latitude, double longitude)
    {
        IReadOnlyCollection<GeofencePoint> points = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking: false);

        // fewer than three points don't make a fence, so there is nothing to enforce
        if (points.Count < 3)
        {
            return new IOffenseService.OffenseEvaluation(IOffenseService.OffenseChange.None, null);
        }

        var polygon = points
            .OrderBy(p => p.SequenceOrder)
            .Select(p => (p.Latitude, p.Longitude))
            .ToList();

        bool isInside = GeofenceEvaluator.IsInsidePolygon(latitude, longitude, polygon);

        var activeOffense = await uow.OffenseRepository.GetActiveOffenseAsync(memberId, OffenseType.OutOfBounds, tracking: true);

        if (!isInside && activeOffense is null)
        {
            var offense = uow.OffenseRepository.AddOffense(
                sessionId,
                memberId,
                OffenseType.OutOfBounds,
                OffenseStatus.Active,
                clock.GetCurrentInstant());

            await uow.SaveChangesAsync();

            logger.LogInformation("Raised out-of-bounds offense {OffenseId} for member {MemberId} in session {SessionId}", offense.Id, memberId, sessionId);

            return new IOffenseService.OffenseEvaluation(IOffenseService.OffenseChange.Raised, offense);
        }

        if (isInside && activeOffense is not null)
        {
            activeOffense.Status = OffenseStatus.Cleared;
            activeOffense.ClearedAt = clock.GetCurrentInstant();

            await uow.SaveChangesAsync();

            logger.LogInformation("Cleared out-of-bounds offense {OffenseId} for member {MemberId} in session {SessionId}", activeOffense.Id, memberId, sessionId);

            return new IOffenseService.OffenseEvaluation(IOffenseService.OffenseChange.Cleared, activeOffense);
        }

        return new IOffenseService.OffenseEvaluation(IOffenseService.OffenseChange.None, null);
    }
}
