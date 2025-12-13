using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.LocationLogs;

public static class LocationLogEndpoint
{
    private const string ApiBasePath = "/api/locationlogs";

    public static void MapLocationLogEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] ILocationLogService service) =>
            {
                IEnumerable<LocationLog> locationLogs = await service.GetAllLocationLogsAsync();

                return Results.Ok(new LocationLogListResponse
                {
                    Items = [.. locationLogs.Select(LocationLogInformationDto.FromLocationLog)]
                });
            })
            .Produces<LocationLogListResponse>(StatusCodes.Status200OK);

        group.MapGet("{logId:guid}", async (
            [FromRoute] Guid logId,
            [FromServices] ILocationLogService service) =>
            {
                OneOf<LocationLog, NotFound> locationLogResult = await service.GetLocationLogByIdAsync(logId);

                return locationLogResult.Match(
                    locationLog => Results.Ok(LocationLogDetailsDto.FromLocationLog(locationLog)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<LocationLogDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] LocationLogAddRequest newLocationLog,
            [FromServices] ILocationLogService service) =>
            {
                OneOf<LocationLog, Error> addResult = await service
                .AddLocationLogAsync(
                    new ILocationLogService.LocationLogData(
                        newLocationLog.MemberId,
                        newLocationLog.Timestamp,
                        newLocationLog.Latitude,
                        newLocationLog.Longitude,
                        newLocationLog.AccuracyMeters,
                        newLocationLog.TransportMode,
                        newLocationLog.IsRevealedPosition
                    )
                );

                return addResult.Match(
                    locationLog => Results.Created($"{ApiBasePath}/{locationLog.LogId}", LocationLogDetailsDto.FromLocationLog(locationLog)),
                    error => Results.BadRequest()
                );
            })
            .Produces<LocationLogDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{logId:guid}", async (
            [FromRoute] Guid logId,
            [FromBody] LocationLogUpdateRequest locationLogUpdate,
            [FromServices] ILocationLogService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdateLocationLogAsync(
                    logId,
                    new ILocationLogService.LocationLogData(
                        locationLogUpdate.MemberId,
                        locationLogUpdate.Timestamp,
                        locationLogUpdate.Latitude,
                        locationLogUpdate.Longitude,
                        locationLogUpdate.AccuracyMeters,
                        locationLogUpdate.TransportMode,
                        locationLogUpdate.IsRevealedPosition
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{logId:guid}", async (
            [FromRoute] Guid logId,
            [FromServices] ILocationLogService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeleteLocationLogAsync(logId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record LocationLogListResponse
    {
        public required IEnumerable<LocationLogInformationDto> Items { get; init; }
    }

    private sealed record LocationLogInformationDto(
        Guid LogId,
        Guid MemberId,
        Instant Timestamp,
        double Latitude,
        double Longitude
    )
    {
        public static LocationLogInformationDto FromLocationLog(LocationLog locationLog) =>
            new(
                locationLog.LogId,
                locationLog.MemberId,
                locationLog.Timestamp,
                locationLog.Latitude,
                locationLog.Longitude
            );
    }

    private sealed record LocationLogDetailsDto(
        Guid LogId,
        Guid MemberId,
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
                locationLog.LogId,
                locationLog.MemberId,
                locationLog.Timestamp,
                locationLog.Latitude,
                locationLog.Longitude,
                locationLog.AccuracyMeters,
                locationLog.TransportMode,
                locationLog.IsRevealedPosition
            );
    }

    private sealed record LocationLogAddRequest(
        Guid MemberId,
        Instant Timestamp,
        double Latitude,
        double Longitude,
        double AccuracyMeters,
        TransportMode TransportMode,
        bool IsRevealedPosition = false
    );

    private sealed record LocationLogUpdateRequest(
        Guid MemberId,
        Instant Timestamp,
        double Latitude,
        double Longitude,
        double AccuracyMeters,
        TransportMode TransportMode,
        bool IsRevealedPosition
    );
}
