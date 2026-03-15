using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions/{sessionId:int}/teams/{teamId:int}/members/{memberId:int}/powerupusages")]
public sealed class PowerUpUsageController(
    ITransactionProvider transaction,
    IPowerUpUsageService powerUpUsageService,
    ILogger<PowerUpUsageController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<PowerUpUsageListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<PowerUpUsageListResponse>> GetAllPowerUpUsages(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId)
    {
        IReadOnlyCollection<PowerUpUsage> usages = await powerUpUsageService.GetUsagesByMemberIdAsync(sessionId, teamId, memberId, tracking: false);

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
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromRoute] int usageId)
    {
        OneOf<PowerUpUsage, NotFound> usageResult = await powerUpUsageService.GetPowerUpUsageByIdAsync(sessionId, teamId, memberId, usageId, tracking: false);

        return usageResult.Match<ActionResult<PowerUpUsageDto>>(
            usage => Ok(PowerUpUsageDto.FromPowerUpUsage(usage)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<PowerUpUsageDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddPowerUpUsage(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromBody] PowerUpUsageAddRequest addRequest)
    {
        if (!ValidateRequest<PowerUpUsageAddRequest.Validator, PowerUpUsageAddRequest>(addRequest))
        {
            logger.LogWarning("Rejected power-up usage create request for member {MemberId} because validation failed", memberId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<PowerUpUsage, NotFound, DomainError> addResult = await powerUpUsageService.AddPowerUpUsageAsync(
                new IPowerUpUsageService.PowerUpUsageData(
                    memberId,
                    addRequest.PowerUpType,
                    addRequest.UsedAt
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async usage =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Created power-up usage {UsageId} for member {MemberId}", usage.Id, memberId);

                return CreatedAtAction(nameof(GetPowerUpUsageById),
                    new { sessionId, teamId, memberId, usageId = usage.Id },
                    PowerUpUsageDto.FromPowerUpUsage(usage));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected power-up usage create request because member {MemberId} was not found in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected power-up usage create request for member {MemberId} with domain error {ErrorCode}: {ErrorMessage}", memberId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
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
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdatePowerUpUsage(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromRoute] int usageId,
        [FromBody] PowerUpUsageUpdateRequest updateRequest)
    {
        if (!ValidateRequest<PowerUpUsageUpdateRequest.Validator, PowerUpUsageUpdateRequest>(updateRequest))
        {
            logger.LogWarning("Rejected power-up usage update request for usage {UsageId} and member {MemberId} because validation failed", usageId, memberId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> updateResult = await powerUpUsageService.UpdatePowerUpUsageAsync(
                sessionId,
                teamId,
                memberId,
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
                logger.LogInformation("Updated power-up usage {UsageId} for member {MemberId}", usageId, memberId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected power-up usage update request because usage {UsageId} or member {MemberId} was not found", usageId, memberId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected power-up usage update request for usage {UsageId} with domain error {ErrorCode}: {ErrorMessage}", usageId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
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
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromRoute] int usageId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await powerUpUsageService.DeletePowerUpUsageAsync(
                sessionId,
                teamId,
                memberId,
                usageId,
                tracking: true
            );

            return await deleteResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Deleted power-up usage {UsageId} for member {MemberId}", usageId, memberId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected power-up usage delete request because usage {UsageId} or member {MemberId} was not found", usageId, memberId);

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
)
{
    public sealed class Validator : AbstractValidator<PowerUpUsageAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PowerUpType).IsInEnum();
        }
    }
}

public sealed record PowerUpUsageUpdateRequest(
    PowerUpType PowerUpType,
    Instant UsedAt
)
{
    public sealed class Validator : AbstractValidator<PowerUpUsageUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PowerUpType).IsInEnum();
        }
    }
}
