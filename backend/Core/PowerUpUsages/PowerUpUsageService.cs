using OneOf;
using OneOf.Types;

namespace XAct.Core.PowerUpUsages;

public interface IPowerUpUsageService
{
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetAllPowerUpUsagesAsync();
    public ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int usageId);
    public ValueTask<OneOf<PowerUpUsage, Error>> AddPowerUpUsageAsync(PowerUpUsageData newPowerUpUsage);
    public ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(int usageId, PowerUpUsageData powerUpUsageData);
    public ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int usageId);

    public sealed record PowerUpUsageData(
        int MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );
}

public sealed class PowerUpUsageService(IDataStorage dataStorage) : IPowerUpUsageService
{
    private static int _nextUsageId = 6;
    private readonly IDataStorage _dataStorage = dataStorage;

    public async ValueTask<IReadOnlyCollection<PowerUpUsage>> GetAllPowerUpUsagesAsync()
    {
        IEnumerable<PowerUpUsage> powerUpUsages = await _dataStorage.GetPowerUpUsagesAsync();

        return [.. powerUpUsages];
    }

    public async ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int usageId)
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
                UsageId = _nextUsageId++,
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

    public async ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(int usageId, IPowerUpUsageService.PowerUpUsageData powerUpUsageData)
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

    public async ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int usageId)
    {
        var powerUpUsage = await GetPowerUpUsageById(usageId);

        if (powerUpUsage is null)
        {
            return new NotFound();
        }

        await _dataStorage.RemovePowerUpUsageAsync(powerUpUsage);

        return new Success();
    }

    private async ValueTask<PowerUpUsage?> GetPowerUpUsageById(int usageId)
    {
        IEnumerable<PowerUpUsage> powerUpUsages = await _dataStorage.GetPowerUpUsagesAsync();

        return powerUpUsages.FirstOrDefault(pu => pu.UsageId == usageId);
    }
}
