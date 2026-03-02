using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

// TODO Review tracking usage

[Route("api/teammembers/{memberId:int}/locationlogs")]
public sealed class LocationLogController(
    ITransactionProvider transaction,
    ILocationLogService locationLogService,
    ILogger<LocationLogController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<LocationLogListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<LocationLogListResponse>> GetAllLocationLogs([FromRoute] int memberId)
    {
        IReadOnlyCollection<LocationLog> locationLogs = await locationLogService.GetLogsByMemberIdAsync(memberId, tracking: false);

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
        [FromRoute] int memberId,
        [FromRoute] int logId)
    {
        OneOf<LocationLog, NotFound> logResult = await locationLogService.GetLocationLogByIdAsync(memberId, logId, tracking: false);

        return logResult.Match<ActionResult<LocationLogDetailsDto>>(
            locationLog => Ok(LocationLogDetailsDto.FromLocationLog(locationLog)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<LocationLogDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddLocationLog(
        [FromRoute] int memberId,
        [FromBody] LocationLogAddRequest addRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<LocationLog, Error> addResult = await locationLogService.AddLocationLogAsync(
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

                return CreatedAtAction(nameof(GetLocationLogById),
                    new { memberId, logId = locationLog.Id },
                    LocationLogDetailsDto.FromLocationLog(locationLog));
            }, async error =>
            {
                await transaction.RollbackAsync();

                return BadRequest();
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
    public async ValueTask<IActionResult> UpdateLocationLog(
        [FromRoute] int memberId,
        [FromRoute] int logId,
        [FromBody] LocationLogUpdateRequest updateRequest)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await locationLogService.UpdateLocationLogAsync(
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

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();

                return NotFound();
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
    public async ValueTask<IActionResult> DeleteLocationLog(
        [FromRoute] int memberId,
        [FromRoute] int logId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await locationLogService.DeleteLocationLogAsync(memberId, logId, tracking: true);

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
);

public sealed record LocationLogUpdateRequest(
    Instant Timestamp,
    double Latitude,
    double Longitude,
    double AccuracyMeters,
    TransportMode TransportMode,
    bool IsRevealedPosition
);
