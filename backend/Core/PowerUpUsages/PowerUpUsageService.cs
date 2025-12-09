using OneOf;
using OneOf.Types;

namespace XAct.Core.PowerUpUsages;

public interface IPowerUpUsageService
{
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetAllPowerUpUsagesAsync();
    public ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(Guid usageId);
    public ValueTask<OneOf<PowerUpUsage, Error>> AddPowerUpUsageAsync(PowerUpUsageData newPowerUpUsage);
    public ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(Guid usageId, PowerUpUsageData powerUpUsageData);
    public ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(Guid usageId);

    public sealed record PowerUpUsageData(
        Guid MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );
}

public sealed class PowerUpUsageService(IDataStorage dataStorage) : IPowerUpUsageService
{
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<PowerUpUsage>> GetAllPowerUpUsagesAsync()
    {
        IEnumerable<PowerUpUsage> powerUpUsages = await _dataStorage.GetPowerUpUsagesAsync();

        return [.. powerUpUsages];
    }

    public async ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(Guid usageId)
    {
        var powerUpUsage = await GetPowerUpUsageById(usageId);

        return powerUpUsage is not null ? powerUpUsage : new NotFound();
    }

    public async ValueTask<OneOf<PowerUpUsage, Error>> AddPowerUpUsageAsync(IPowerUpUsageService.PowerUpUsageData newPowerUpUsage)
    {
        try
        {
            var powerUpUsage = new PowerUpUsage
            {
                UsageId = Guid.NewGuid(),
                MemberId = newPowerUpUsage.MemberId,
                PowerUpType = newPowerUpUsage.PowerUpType,
                UsedAt = newPowerUpUsage.UsedAt
            };

            await _dataStorage.AddPowerUpUsageAsync(powerUpUsage);

            return powerUpUsage;
        }
        catch (Exception)
        {
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(Guid usageId, IPowerUpUsageService.PowerUpUsageData powerUpUsageData)
    {
        var powerUpUsage = await GetPowerUpUsageById(usageId);

        if (powerUpUsage is null)
        {
            return new NotFound();
        }

        powerUpUsage.MemberId = powerUpUsageData.MemberId;
        powerUpUsage.PowerUpType = powerUpUsageData.PowerUpType;
        powerUpUsage.UsedAt = powerUpUsageData.UsedAt;

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(Guid usageId)
    {
        var powerUpUsage = await GetPowerUpUsageById(usageId);

        if (powerUpUsage is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemovePowerUpUsageAsync(powerUpUsage);

        return new Success();
    }

    private async ValueTask<PowerUpUsage?> GetPowerUpUsageById(Guid usageId)
    {
        IEnumerable<PowerUpUsage> powerUpUsages = await _dataStorage.GetPowerUpUsagesAsync();

        return powerUpUsages.FirstOrDefault(pu => pu.UsageId == usageId);
    }
}
