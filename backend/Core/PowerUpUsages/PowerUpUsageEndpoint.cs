using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.PowerUpUsages;

public static class PowerUpUsageEndpoint
{
    private const string ApiBasePath = "/api/powerupusages";

    public static void MapPowerUpUsageEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] IPowerUpUsageService service) =>
            {
                IEnumerable<PowerUpUsage> powerUpUsages = await service.GetAllPowerUpUsagesAsync();

                return Results.Ok(new PowerUpUsageListResponse
                {
                    Items = [.. powerUpUsages.Select(PowerUpUsageInformationDto.FromPowerUpUsage)]
                });
            })
            .Produces<PowerUpUsageListResponse>(StatusCodes.Status200OK);

        group.MapGet("{usageId:guid}", async (
            [FromRoute] Guid usageId,
            [FromServices] IPowerUpUsageService service) =>
            {
                OneOf<PowerUpUsage, NotFound> powerUpUsageResult = await service.GetPowerUpUsageByIdAsync(usageId);

                return powerUpUsageResult.Match(
                    powerUpUsage => Results.Ok(PowerUpUsageDetailsDto.FromPowerUpUsage(powerUpUsage)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<PowerUpUsageDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] PowerUpUsageAddRequest newPowerUpUsage,
            [FromServices] IPowerUpUsageService service) =>
            {
                OneOf<PowerUpUsage, Error> addResult = await service
                .AddPowerUpUsageAsync(
                    new IPowerUpUsageService.PowerUpUsageData(
                        newPowerUpUsage.MemberId,
                        newPowerUpUsage.PowerUpType,
                        newPowerUpUsage.UsedAt
                    )
                );

                return addResult.Match(
                    powerUpUsage => Results.Created($"{ApiBasePath}/{powerUpUsage.UsageId}", PowerUpUsageDetailsDto.FromPowerUpUsage(powerUpUsage)),
                    error => Results.BadRequest()
                );
            })
            .Produces<PowerUpUsageDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{usageId:guid}", async (
            [FromRoute] Guid usageId,
            [FromBody] PowerUpUsageUpdateRequest powerUpUsageUpdate,
            [FromServices] IPowerUpUsageService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdatePowerUpUsageAsync(
                    usageId,
                    new IPowerUpUsageService.PowerUpUsageData(
                        powerUpUsageUpdate.MemberId,
                        powerUpUsageUpdate.PowerUpType,
                        powerUpUsageUpdate.UsedAt
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{usageId:guid}", async (
            [FromRoute] Guid usageId,
            [FromServices] IPowerUpUsageService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeletePowerUpUsageAsync(usageId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record PowerUpUsageListResponse
    {
        public required IEnumerable<PowerUpUsageInformationDto> Items { get; init; }
    }

    private sealed record PowerUpUsageInformationDto(
        Guid UsageId,
        Guid MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    )
    {
        public static PowerUpUsageInformationDto FromPowerUpUsage(PowerUpUsage powerUpUsage) =>
            new(
                powerUpUsage.UsageId,
                powerUpUsage.MemberId,
                powerUpUsage.PowerUpType,
                powerUpUsage.UsedAt
            );
    }

    private sealed record PowerUpUsageDetailsDto(
        Guid UsageId,
        Guid MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    )
    {
        public static PowerUpUsageDetailsDto FromPowerUpUsage(PowerUpUsage powerUpUsage) =>
            new(
                powerUpUsage.UsageId,
                powerUpUsage.MemberId,
                powerUpUsage.PowerUpType,
                powerUpUsage.UsedAt
            );
    }

    private sealed record PowerUpUsageAddRequest(
        Guid MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );

    private sealed record PowerUpUsageUpdateRequest(
        Guid MemberId,
        PowerUpType PowerUpType,
        Instant UsedAt
    );
}
