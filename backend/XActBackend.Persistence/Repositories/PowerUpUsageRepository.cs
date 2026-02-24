using Microsoft.EntityFrameworkCore;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Repositories;

public interface IPowerUpUsageRepository
{
    public PowerUpUsage AddPowerUpUsage(int memberId, PowerUpType powerUpType, Instant usedAt);
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int memberId, bool tracking);
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

    public void RemovePowerUpUsage(PowerUpUsage usage)
    {
        usageSet.Remove(usage);
    }
}
