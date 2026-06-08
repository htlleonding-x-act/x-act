using XActBackend.Core.Util;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

/// <summary>
///     Detects and tracks automatic rule violations (offenses) of team members, such as leaving the
///     session's geofenced game area. Active offenses feed the report tab's "flagged players" list.
/// </summary>
public interface IOffenseService
{
    /// <summary>
    ///     Get all currently active offenses for a session.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All active offenses for the session</returns>
    public ValueTask<IReadOnlyCollection<Offense>> GetActiveOffensesBySessionAsync(int sessionId, bool tracking);

    /// <summary>
    ///     Evaluate a member's latest position against the session geofence and raise or clear an
    ///     out-of-bounds offense as needed. Safe to call after every recorded location ping.
    /// </summary>
    /// <param name="sessionId">The id of the session</param>
    /// <param name="memberId">The id of the member that reported the position</param>
    /// <param name="latitude">The member's latitude</param>
    /// <param name="longitude">The member's longitude</param>
    /// <returns>The change that occurred and the affected offense, if any</returns>
    public ValueTask<OffenseEvaluation> EvaluateMemberLocationAsync(int sessionId, int memberId, double latitude, double longitude);

    /// <summary>The kind of change produced by evaluating a member's location.</summary>
    public enum OffenseChange
    {
        None,
        Raised,
        Cleared,
    }

    /// <summary>The outcome of an offense evaluation.</summary>
    /// <param name="Change">What happened (nothing, a new offense, or a cleared one)</param>
    /// <param name="Offense">The affected offense, set when <paramref name="Change"/> is not None</param>
    public sealed record OffenseEvaluation(OffenseChange Change, Offense? Offense);
}

internal sealed class OffenseService(IUnitOfWork uow, IClock clock, ILogger<OffenseService> logger) : IOffenseService
{
    public async ValueTask<IReadOnlyCollection<Offense>> GetActiveOffensesBySessionAsync(int sessionId, bool tracking) =>
        await uow.OffenseRepository.GetActiveOffensesBySessionAsync(sessionId, tracking);

    public async ValueTask<IOffenseService.OffenseEvaluation> EvaluateMemberLocationAsync(int sessionId, int memberId, double latitude, double longitude)
    {
        IReadOnlyCollection<GeofencePoint> points = await uow.GeofencePointRepository.GetPointsBySessionIdAsync(sessionId, tracking: false);

        // No usable fence -> nothing to enforce.
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
