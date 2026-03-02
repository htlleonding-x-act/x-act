using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

// TODO Review tracking usage

[Route("api/teammembers/{memberId:int}/powerupusages")]
public sealed class PowerUpUsageController(
    ITransactionProvider transaction,
    IPowerUpUsageService powerUpUsageService,
    ILogger<PowerUpUsageController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<PowerUpUsageListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<PowerUpUsageListResponse>> GetAllPowerUpUsages([FromRoute] int memberId)
    {
        IReadOnlyCollection<PowerUpUsage> usages = await powerUpUsageService.GetUsagesByMemberIdAsync(memberId, tracking: false);

        return Ok(new PowerUpUsageListResponse
        {
            Items = usages.Select(PowerUpUsageDto.FromPowerUpUsage).ToList()
        });
    }

    [HttpGet]
    [Route("{usageId:int}")]
    [ProducesResponseType<PowerUpUsageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PowerUpUsageDto>> GetPowerUpUsageById(
        [FromRoute] int memberId,
        [FromRoute] int usageId)
    {
        OneOf<PowerUpUsage, NotFound> usageResult = await powerUpUsageService.GetPowerUpUsageByIdAsync(memberId, usageId, tracking: false);

        return usageResult.Match<ActionResult<PowerUpUsageDto>>(
            usage => Ok(PowerUpUsageDto.FromPowerUpUsage(usage)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<PowerUpUsageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddPowerUpUsage(
        [FromRoute] int memberId,
        [FromBody] PowerUpUsageAddRequest addRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<PowerUpUsage, Error> addResult = await powerUpUsageService.AddPowerUpUsageAsync(
                new IPowerUpUsageService.PowerUpUsageData(
                    memberId,
                    addRequest.PowerUpType,
                    addRequest.UsedAt
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async usage =>
            {
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetPowerUpUsageById),
                    new { memberId, usageId = usage.Id },
                    PowerUpUsageDto.FromPowerUpUsage(usage));
            }, async error =>
            {
                await transaction.RollbackAsync();

                return BadRequest();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add power-up usage for member {MemberId}", memberId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPut]
    [Route("{usageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> UpdatePowerUpUsage(
        [FromRoute] int memberId,
        [FromRoute] int usageId,
        [FromBody] PowerUpUsageUpdateRequest updateRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await powerUpUsageService.UpdatePowerUpUsageAsync(
                usageId,
                new IPowerUpUsageService.PowerUpUsageData(
                    memberId,
                    updateRequest.PowerUpType,
                    updateRequest.UsedAt
                ),
                tracking: true
            );

            return await updateResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();

                return NotFound();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update power-up usage {UsageId} for member {MemberId}", usageId, memberId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpDelete]
    [Route("{usageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeletePowerUpUsage(
        [FromRoute] int memberId,
        [FromRoute] int usageId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await powerUpUsageService.DeletePowerUpUsageAsync(memberId, usageId, tracking: true);

            return await deleteResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();

                return NotFound();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete power-up usage {UsageId} for member {MemberId}", usageId, memberId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }
}

public sealed class PowerUpUsageListResponse
{
    public required List<PowerUpUsageDto> Items { get; init; }
}

public sealed record PowerUpUsageDto(
    int Id,
    int MemberId,
    PowerUpType PowerUpType,
    Instant UsedAt
)
{
    public static PowerUpUsageDto FromPowerUpUsage(PowerUpUsage usage) =>
        new(
            usage.Id,
            usage.MemberId,
            usage.PowerUpType,
            usage.UsedAt
        );
}

public sealed record PowerUpUsageAddRequest(
    PowerUpType PowerUpType,
    Instant UsedAt
);

public sealed record PowerUpUsageUpdateRequest(
    PowerUpType PowerUpType,
    Instant UsedAt
);
