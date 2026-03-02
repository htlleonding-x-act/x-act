using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IPowerUpUsageService
{
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int memberId, bool tracking);
    public ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int memberId, int usageId, bool tracking);
    public ValueTask<OneOf<PowerUpUsage, Error>> AddPowerUpUsageAsync(PowerUpUsageData newPowerUpUsage);
    public ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(int usageId, PowerUpUsageData powerUpUsageData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int memberId, int usageId, bool tracking);

    public sealed record PowerUpUsageData(
        int MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );
}

internal sealed class PowerUpUsageService(IUnitOfWork uow) : IPowerUpUsageService
{
    public async ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int memberId, bool tracking)
    {
        IEnumerable<PowerUpUsage> powerUpUsages = await uow.PowerUpUsageRepository.GetUsagesByMemberIdAsync(memberId, tracking);
        return [.. powerUpUsages];
    }

    public async ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int memberId, int usageId, bool tracking)
    {
        IReadOnlyCollection<PowerUpUsage> powerUpUsages = await uow.PowerUpUsageRepository.GetUsagesByMemberIdAsync(memberId, tracking);
        var powerUpUsage = powerUpUsages.FirstOrDefault(u => u.Id == usageId);

        return powerUpUsage is not null ? powerUpUsage : new NotFound();
    }

    public async ValueTask<OneOf<PowerUpUsage, Error>> AddPowerUpUsageAsync(IPowerUpUsageService.PowerUpUsageData newPowerUpUsage)
    {
        try
        {
            var powerUpUsage = uow.PowerUpUsageRepository.AddPowerUpUsage(
                newPowerUpUsage.MemberId,
                newPowerUpUsage.PowerUpType,
                newPowerUpUsage.UsedAt);

            await uow.SaveChangesAsync();

            return powerUpUsage;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(int usageId, IPowerUpUsageService.PowerUpUsageData powerUpUsageData, bool tracking)
    {
        IReadOnlyCollection<PowerUpUsage> powerUpUsages = await uow.PowerUpUsageRepository.GetUsagesByMemberIdAsync(powerUpUsageData.MemberId, tracking);
        var powerUpUsage = powerUpUsages.FirstOrDefault(u => u.Id == usageId);

        if (powerUpUsage is null)
        {
            return new NotFound();
        }

        powerUpUsage.PowerUpType = powerUpUsageData.PowerUpType;
        powerUpUsage.UsedAt = powerUpUsageData.UsedAt;

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int memberId, int usageId, bool tracking)
    {
        IReadOnlyCollection<PowerUpUsage> powerUpUsages = await uow.PowerUpUsageRepository.GetUsagesByMemberIdAsync(memberId, tracking);
        var usage = powerUpUsages.FirstOrDefault(u => u.Id == usageId);

        if (usage is null)
        {
            return new NotFound();
        }

        uow.PowerUpUsageRepository.RemovePowerUpUsage(usage);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
