using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.GeofencePoints;

public static class GeofencePointEndpoint
{
    private const string ApiBasePath = "/api/geofencepoints";

    public static void MapGeofencePointEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] IGeofencePointService service) =>
            {
                IEnumerable<GeofencePoint> geofencePoints = await service.GetAllGeofencePointsAsync();

                return Results.Ok(new GeofencePointListResponse
                {
                    Items = [.. geofencePoints.Select(GeofencePointInformationDto.FromGeofencePoint)]
                });
            })
            .Produces<GeofencePointListResponse>(StatusCodes.Status200OK);

        group.MapGet("{pointId:guid}", async (
            [FromRoute] Guid pointId,
            [FromServices] IGeofencePointService service) =>
            {
                OneOf<GeofencePoint, NotFound> geofencePointResult = await service.GetGeofencePointByIdAsync(pointId);

                return geofencePointResult.Match(
                    geofencePoint => Results.Ok(GeofencePointDetailsDto.FromGeofencePoint(geofencePoint)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<GeofencePointDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] GeofencePointAddRequest newGeofencePoint,
            [FromServices] IGeofencePointService service) =>
            {
                OneOf<GeofencePoint, Error> addResult = await service
                .AddGeofencePointAsync(
                    new IGeofencePointService.GeofencePointData(
                        newGeofencePoint.SessionId,
                        newGeofencePoint.Latitude,
                        newGeofencePoint.Longitude,
                        newGeofencePoint.SequenceOrder
                    )
                );

                return addResult.Match(
                    geofencePoint => Results.Created($"{ApiBasePath}/{geofencePoint.PointId}", GeofencePointDetailsDto.FromGeofencePoint(geofencePoint)),
                    error => Results.BadRequest()
                );
            })
            .Produces<GeofencePointDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{pointId:guid}", async (
            [FromRoute] Guid pointId,
            [FromBody] GeofencePointUpdateRequest geofencePointUpdate,
            [FromServices] IGeofencePointService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdateGeofencePointAsync(
                    pointId,
                    new IGeofencePointService.GeofencePointData(
                        geofencePointUpdate.SessionId,
                        geofencePointUpdate.Latitude,
                        geofencePointUpdate.Longitude,
                        geofencePointUpdate.SequenceOrder
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{pointId:guid}", async (
            [FromRoute] Guid pointId,
            [FromServices] IGeofencePointService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeleteGeofencePointAsync(pointId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record GeofencePointListResponse
    {
        public required IEnumerable<GeofencePointInformationDto> Items { get; init; }
    }

    private sealed record GeofencePointInformationDto(
        Guid PointId,
        Guid SessionId,
        double Latitude,
        double Longitude
    )
    {
        public static GeofencePointInformationDto FromGeofencePoint(GeofencePoint geofencePoint) =>
            new(
                geofencePoint.PointId,
                geofencePoint.SessionId,
                geofencePoint.Latitude,
                geofencePoint.Longitude
            );
    }

    private sealed record GeofencePointDetailsDto(
        Guid PointId,
        Guid SessionId,
        double Latitude,
        double Longitude,
        int SequenceOrder
    )
    {
        public static GeofencePointDetailsDto FromGeofencePoint(GeofencePoint geofencePoint) =>
            new(
                geofencePoint.PointId,
                geofencePoint.SessionId,
                geofencePoint.Latitude,
                geofencePoint.Longitude,
                geofencePoint.SequenceOrder
            );
    }

    private sealed record GeofencePointAddRequest(
        Guid SessionId,
        double Latitude,
        double Longitude,
        int SequenceOrder
    );

    private sealed record GeofencePointUpdateRequest(
        Guid SessionId,
        double Latitude,
        double Longitude,
        int SequenceOrder
    );
}
