using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions/{sessionId:int}/geofencepoints")]
public sealed class GeofencePointController(
    ITransactionProvider transaction,
    IGeofencePointService geofencePointService,
    ILogger<GeofencePointController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<GeofencePointListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<GeofencePointListResponse>> GetAllGeofencePoints([FromRoute] int sessionId)
    {
        IReadOnlyCollection<GeofencePoint> geofencePoints = await geofencePointService.GetAllPointsBySessionIdAsync(sessionId, tracking: false);

        return Ok(new GeofencePointListResponse
        {
            Items = geofencePoints.Select(GeofencePointInformationDto.FromGeofencePoint).ToList()
        });
    }

    [HttpGet]
    [Route("{pointId:int}")]
    [ProducesResponseType<GeofencePointDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<GeofencePointDetailsDto>> GetGeofencePointById(
        [FromRoute] int sessionId,
        [FromRoute] int pointId)
    {
        OneOf<GeofencePoint, NotFound> pointResult = await geofencePointService.GetGeofencePointByIdAsync(sessionId, pointId, tracking: false);

        return pointResult.Match<ActionResult<GeofencePointDetailsDto>>(
            geofencePoint => Ok(GeofencePointDetailsDto.FromGeofencePoint(geofencePoint)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<GeofencePointDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<IActionResult> AddGeofencePoint(
        [FromRoute] int sessionId,
        [FromBody] GeofencePointAddRequest addRequest)
    {
        if (!ValidateRequest<GeofencePointAddRequest.Validator, GeofencePointAddRequest>(addRequest))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<GeofencePoint, NotFound, DomainError> addResult = await geofencePointService.AddGeofencePointAsync(
                new IGeofencePointService.GeofencePointData(
                    sessionId,
                    addRequest.Latitude,
                    addRequest.Longitude,
                    addRequest.SequenceOrder
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async geofencePoint =>
            {
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetGeofencePointById),
                    new { sessionId, pointId = geofencePoint.Id },
                    GeofencePointDetailsDto.FromGeofencePoint(geofencePoint));
            }, async notFound =>
            {
                await transaction.RollbackAsync();
                logger.LogWarning("Rejected geofence point create request because session {SessionId} was not found", sessionId);

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add geofence point for session {SessionId}", sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPut]
    [Route("{pointId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> UpdateGeofencePoint(
        [FromRoute] int sessionId,
        [FromRoute] int pointId,
        [FromBody] GeofencePointUpdateRequest updateRequest)
    {
        if (!ValidateRequest<GeofencePointUpdateRequest.Validator, GeofencePointUpdateRequest>(updateRequest))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await geofencePointService.UpdateGeofencePointAsync(
                pointId,
                new IGeofencePointService.GeofencePointData(
                    sessionId,
                    updateRequest.Latitude,
                    updateRequest.Longitude,
                    updateRequest.SequenceOrder
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
            logger.LogError(ex, "Failed to update geofence point {PointId} for session {SessionId}", pointId, sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpDelete]
    [Route("{pointId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteGeofencePoint(
        [FromRoute] int sessionId,
        [FromRoute] int pointId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await geofencePointService.DeleteGeofencePointAsync(sessionId, pointId, tracking: true);

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
            logger.LogError(ex, "Failed to delete geofence point {PointId} for session {SessionId}", pointId, sessionId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }
}

public sealed class GeofencePointListResponse
{
    public required List<GeofencePointInformationDto> Items { get; init; }
}

public sealed record GeofencePointInformationDto(
    int Id,
    int SessionId,
    double Latitude,
    double Longitude
)
{
    public static GeofencePointInformationDto FromGeofencePoint(GeofencePoint geofencePoint) =>
        new(
            geofencePoint.Id,
            geofencePoint.SessionId,
            geofencePoint.Latitude,
            geofencePoint.Longitude
        );
}

public sealed record GeofencePointDetailsDto(
    int Id,
    int SessionId,
    double Latitude,
    double Longitude,
    int SequenceOrder
)
{
    public static GeofencePointDetailsDto FromGeofencePoint(GeofencePoint geofencePoint) =>
        new(
            geofencePoint.Id,
            geofencePoint.SessionId,
            geofencePoint.Latitude,
            geofencePoint.Longitude,
            geofencePoint.SequenceOrder
        );
}

public sealed record GeofencePointAddRequest(
    double Latitude,
    double Longitude,
    int SequenceOrder
)
{
    public sealed class Validator : AbstractValidator<GeofencePointAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.SequenceOrder).GreaterThanOrEqualTo(0);
        }
    }
}

public sealed record GeofencePointUpdateRequest(
    double Latitude,
    double Longitude,
    int SequenceOrder
)
{
    public sealed class Validator : AbstractValidator<GeofencePointUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
            RuleFor(x => x.SequenceOrder).GreaterThanOrEqualTo(0);
        }
    }
}
