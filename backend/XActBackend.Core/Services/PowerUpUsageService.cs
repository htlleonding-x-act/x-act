using OneOf;
using OneOf.Types;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Core.Services;

public interface IPowerUpUsageService
{
    public ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking);
    public ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking);
    public ValueTask<OneOf<PowerUpUsage, Error>> AddPowerUpUsageAsync(PowerUpUsageData newPowerUpUsage);
    public ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, PowerUpUsageData powerUpUsageData, bool tracking);
    public ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking);

    public sealed record PowerUpUsageData(
        int MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );
}

internal sealed class PowerUpUsageService(IUnitOfWork uow, ILogger<PowerUpUsageService> logger) : IPowerUpUsageService
{
    public async ValueTask<IReadOnlyCollection<PowerUpUsage>> GetUsagesByMemberIdAsync(int sessionId, int teamId, int memberId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return [];
        }

        IEnumerable<PowerUpUsage> powerUpUsages = await uow.PowerUpUsageRepository.GetUsagesByMemberIdAsync(memberId, tracking);
        return [.. powerUpUsages];
    }

    public async ValueTask<OneOf<PowerUpUsage, NotFound>> GetPowerUpUsageByIdAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var powerUpUsage = await uow.PowerUpUsageRepository.GetUsageByMemberAndIdAsync(memberId, usageId, tracking);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add power-up usage for member {MemberId}", newPowerUpUsage.MemberId);
            return new Error();
        }
    }

    public async ValueTask<OneOf<Success, NotFound>> UpdatePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, IPowerUpUsageService.PowerUpUsageData powerUpUsageData, bool tracking)
    {
        if (powerUpUsageData.MemberId != memberId)
        {
            return new NotFound();
        }

        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var powerUpUsage = await uow.PowerUpUsageRepository.GetUsageByMemberAndIdAsync(memberId, usageId, tracking);

        if (powerUpUsage is null)
        {
            return new NotFound();
        }

        powerUpUsage.PowerUpType = powerUpUsageData.PowerUpType;
        powerUpUsage.UsedAt = powerUpUsageData.UsedAt;

        await uow.SaveChangesAsync();

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound>> DeletePowerUpUsageAsync(int sessionId, int teamId, int memberId, int usageId, bool tracking)
    {
        var member = await uow.TeamMemberRepository.GetMemberBySessionAndTeamIdAsync(sessionId, teamId, memberId, tracking: false);
        if (member is null)
        {
            return new NotFound();
        }

        var usage = await uow.PowerUpUsageRepository.GetUsageByMemberAndIdAsync(memberId, usageId, tracking);

        if (usage is null)
        {
            return new NotFound();
        }

        uow.PowerUpUsageRepository.RemovePowerUpUsage(usage);
        await uow.SaveChangesAsync();

        return new Success();
    }
}
