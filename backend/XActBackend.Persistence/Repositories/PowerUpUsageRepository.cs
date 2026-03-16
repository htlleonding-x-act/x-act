using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

/// <summary>
///     Repository for <see cref="PowerUpUsage"/> entities.
/// </summary>
public interface IPowerUpUsageRepository
{
    /// <summary>
    ///     Add a new power-up usage event for a team member.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="powerUpType">The used power-up type</param>
    /// <param name="usedAt">Timestamp of usage</param>
    /// <returns>The created tracked power-up usage entity</returns>
    public PowerUpUsage AddPowerUpUsage(int memberId, PowerUpType powerUpType, Instant usedAt);

    /// <summary>
    ///     Get all power-up usages of a team member.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="tracking">Flag indicating if entities should be tracked by the context</param>
    /// <returns>All power-up usages for the member</returns>
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int memberId, bool tracking);

    /// <summary>
    ///     Get a power-up usage by member id and usage id.
    /// </summary>
    /// <param name="memberId">The id of the member</param>
    /// <param name="usageId">The id of the usage event</param>
    /// <param name="tracking">Flag indicating if the entity should be tracked by the context</param>
    /// <returns>The power-up usage, if found</returns>
    public ValueTask<PowerUpUsage?> GetUsageByMemberAndIdAsync(int memberId, int usageId, bool tracking);

    /// <summary>
    ///     Remove a power-up usage from the repository.
    /// </summary>
    /// <param name="usage">The power-up usage to remove</param>
    public void RemovePowerUpUsage(PowerUpUsage usage);
}

internal sealed class PowerUpUsageRepository(DbSet<PowerUpUsage> usageSet) : IPowerUpUsageRepository
{
    private IQueryable<PowerUpUsage> Usages => usageSet;
    private IQueryable<PowerUpUsage> UsagesNoTracking => Usages.AsNoTracking();

    public PowerUpUsage AddPowerUpUsage(int memberId, PowerUpType powerUpType, Instant usedAt)
    {
        var usage = new PowerUpUsage
        {
            MemberId = memberId,
            PowerUpType = powerUpType,
            UsedAt = usedAt,
        };

        usageSet.Add(usage);

        return usage;
    }

    public async ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int memberId, bool tracking)
    {
        IQueryable<PowerUpUsage> source = tracking ? Usages : UsagesNoTracking;

        List<PowerUpUsage> usages = await source
            .Where(u => u.MemberId == memberId)
            .OrderBy(u => u.UsedAt)
            .ToListAsync();

        return usages;
    }

    public async ValueTask<PowerUpUsage?> GetUsageByMemberAndIdAsync(int memberId, int usageId, bool tracking)
    {
        IQueryable<PowerUpUsage> source = tracking ? Usages : UsagesNoTracking;

        return await source.FirstOrDefaultAsync(u => u.MemberId == memberId && u.Id == usageId);
    }

    public void RemovePowerUpUsage(PowerUpUsage usage)
    {
        usageSet.Remove(usage);
    }
}
