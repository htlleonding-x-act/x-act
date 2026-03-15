using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

// TODO Review tracking usage

[Route("api/gamesessions/{sessionId:int}/teams/{teamId:int}/members/{memberId:int}/locationlogs")]
public sealed class LocationLogController(
    ITransactionProvider transaction,
    ILocationLogService locationLogService,
    ILogger<LocationLogController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<LocationLogListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<LocationLogListResponse>> GetAllLocationLogs(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId)
    {
        IReadOnlyCollection<LocationLog> locationLogs = await locationLogService.GetLogsByMemberIdAsync(sessionId, teamId, memberId, tracking: false);

        return Ok(new LocationLogListResponse
        {
            Items = locationLogs.Select(LocationLogInformationDto.FromLocationLog).ToList()
        });
    }

    [HttpGet]
    [Route("{logId:int}")]
    [ProducesResponseType<LocationLogDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<LocationLogDetailsDto>> GetLocationLogById(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromRoute] int logId)
    {
        OneOf<LocationLog, NotFound> logResult = await locationLogService.GetLocationLogByIdAsync(sessionId, teamId, memberId, logId, tracking: false);

        return logResult.Match<ActionResult<LocationLogDetailsDto>>(
            locationLog => Ok(LocationLogDetailsDto.FromLocationLog(locationLog)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<LocationLogDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddLocationLog(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromBody] LocationLogAddRequest addRequest)
    {
        if (!ValidateRequest<LocationLogAddRequest.Validator, LocationLogAddRequest>(addRequest))
        {
            logger.LogWarning("Rejected location log create request for member {MemberId} because validation failed", memberId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<LocationLog, NotFound, DomainError> addResult = await locationLogService.AddLocationLogAsync(
                new ILocationLogService.LocationLogData(
                    memberId,
                    addRequest.Timestamp,
                    addRequest.Latitude,
                    addRequest.Longitude,
                    addRequest.AccuracyMeters,
                    addRequest.TransportMode,
                    addRequest.IsRevealedPosition
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async locationLog =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Created location log {LogId} for member {MemberId}", locationLog.Id, memberId);

                return CreatedAtAction(nameof(GetLocationLogById),
                    new { sessionId, teamId, memberId, logId = locationLog.Id },
                    LocationLogDetailsDto.FromLocationLog(locationLog));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected location log create request because member {MemberId} was not found in session {SessionId}, team {TeamId}", memberId, sessionId, teamId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected location log create request for member {MemberId} with domain error {ErrorCode}: {ErrorMessage}", memberId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add location log for member {MemberId}", memberId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPut]
    [Route("{logId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdateLocationLog(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromRoute] int logId,
        [FromBody] LocationLogUpdateRequest updateRequest)
    {
        if (!ValidateRequest<LocationLogUpdateRequest.Validator, LocationLogUpdateRequest>(updateRequest))
        {
            logger.LogWarning("Rejected location log update request for log {LogId} and member {MemberId} because validation failed", logId, memberId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> updateResult = await locationLogService.UpdateLocationLogAsync(
                sessionId,
                teamId,
                memberId,
                logId,
                new ILocationLogService.LocationLogData(
                    memberId,
                    updateRequest.Timestamp,
                    updateRequest.Latitude,
                    updateRequest.Longitude,
                    updateRequest.AccuracyMeters,
                    updateRequest.TransportMode,
                    updateRequest.IsRevealedPosition
                ),
                tracking: true
            );

            return await updateResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Updated location log {LogId} for member {MemberId}", logId, memberId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected location log update request because log {LogId} or member {MemberId} was not found", logId, memberId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected location log update request for log {LogId} with domain error {ErrorCode}: {ErrorMessage}", logId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update location log {LogId} for member {MemberId}", logId, memberId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpDelete]
    [Route("{logId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> DeleteLocationLog(
        [FromRoute] int sessionId,
        [FromRoute] int teamId,
        [FromRoute] int memberId,
        [FromRoute] int logId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> deleteResult = await locationLogService.DeleteLocationLogAsync(
                sessionId,
                teamId,
                memberId,
                logId,
                tracking: true
            );

            return await deleteResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();
                logger.LogInformation("Deleted location log {LogId} for member {MemberId}", logId, memberId);

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected location log delete request because log {LogId} or member {MemberId} was not found", logId, memberId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected location log delete request for log {LogId} with domain error {ErrorCode}: {ErrorMessage}", logId, domainError.Code, domainError.Message);

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete location log {LogId} for member {MemberId}", logId, memberId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }
}

public sealed class LocationLogListResponse
{
    public required List<LocationLogInformationDto> Items { get; init; }
}

public sealed record LocationLogInformationDto(
    int Id,
    int MemberId,
    Instant Timestamp,
    double Latitude,
    double Longitude
)
{
    public static LocationLogInformationDto FromLocationLog(LocationLog locationLog) =>
        new(
            locationLog.Id,
            locationLog.MemberId,
            locationLog.Timestamp,
            locationLog.Latitude,
            locationLog.Longitude
        );
}

public sealed record LocationLogDetailsDto(
    int Id,
    int MemberId,
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode TransportMode,
    bool IsRevealedPosition
)
{
    public static LocationLogDetailsDto FromLocationLog(LocationLog locationLog) =>
        new(
            locationLog.Id,
            locationLog.MemberId,
            locationLog.Timestamp,
            locationLog.Latitude,
            locationLog.Longitude,
            locationLog.AccuracyMeters,
            locationLog.TransportMode,
            locationLog.IsRevealedPosition
        );
}

public sealed record LocationLogAddRequest(
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode TransportMode,
    bool IsRevealedPosition = false
)
{
    public sealed class Validator : AbstractValidator<LocationLogAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TransportMode).IsInEnum();
        }
    }
}

public sealed record LocationLogUpdateRequest(
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode TransportMode,
    bool IsRevealedPosition
)
{
    public sealed class Validator : AbstractValidator<LocationLogUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TransportMode).IsInEnum();
        }
    }
}
