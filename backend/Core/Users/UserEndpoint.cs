using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace XAct.Core.Users;

public static class UserEndpoint
{
    private const string ApiBasePath = "/api/users";

    public static void MapUserEndpoint(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiBasePath);

        group.MapGet("", async (
            [FromServices] IUserService service) =>
            {
                IEnumerable<User> users = await service.GetAllUsersAsync();

                return Results.Ok(new UserListResponse
                {
                    Items = [.. users.Select(UserInformationDto.FromUser)]
                });
            })
            .Produces<UserListResponse>(StatusCodes.Status200OK);

        group.MapGet("{userId:int}", async (
            [FromRoute] int userId,
            [FromServices] IUserService service) =>
            {
                OneOf<User, NotFound> userResult = await service.GetUserByIdAsync(userId);

                return userResult.Match(
                    user => Results.Ok(UserDetailsDto.FromUser(user)),
                    notFound => Results.NotFound()
                );
            })
            .Produces<UserDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            [FromBody] UserAddRequest newUser,
            [FromServices] IUserService service) =>
            {
                OneOf<User, Error> addResult = await service
                .AddUserAsync(
                    new IUserService.UserData(
                        newUser.Username,
                        newUser.Email,
                        newUser.PasswordHash,
                        newUser.AccountType,
                        newUser.SubscriptionEndDate,
                        newUser.TotalWins,
                        newUser.TotalGamesPlayed
                    )
                );

                return addResult.Match(
                    user => Results.Created($"{ApiBasePath}/{user.UserId}", UserDetailsDto.FromUser(user)),
                    error => Results.BadRequest()
                );
            })
            .Produces<UserDetailsDto>(StatusCodes.Status201Created)
            .Produces<string>(StatusCodes.Status400BadRequest);

        group.MapPut("{userId:int}", async (
            [FromRoute] int userId,
            [FromBody] UserUpdateRequest userUpdate,
            [FromServices] IUserService service) =>
            {
                OneOf<Success, NotFound> updateResult = await service
                .UpdateUserAsync(
                    userId,
                    new IUserService.UserData(
                        userUpdate.Username,
                        userUpdate.Email,
                        userUpdate.PasswordHash,
                        userUpdate.AccountType,
                        userUpdate.SubscriptionEndDate,
                        userUpdate.TotalWins,
                        userUpdate.TotalGamesPlayed
                    )
                );

                return updateResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("{userId:int}", async (
            [FromRoute] int userId,
            [FromServices] IUserService service) =>
            {
                OneOf<Success, NotFound> deleteResult = await service.DeleteUserAsync(userId);

                return deleteResult.Match(
                    success => Results.NoContent(),
                    notFound => Results.NotFound()
                );
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private sealed record UserListResponse
    {
        public required IEnumerable<UserInformationDto> Items { get; init; }
    }

    private sealed record UserInformationDto(
        int UserId,
        string Username,
        string Email,
        AccountType AccountType
    )
    {
        public static UserInformationDto FromUser(User user) =>
            new(
                user.UserId,
                user.Username,
                user.Email,
                user.AccountType
            );
    }

    private sealed record UserDetailsDto(
        int UserId,
        string Username,
        string Email,
        string PasswordHash,
        AccountType AccountType,
        Instant? SubscriptionEndDate,
        int TotalWins,
        int TotalGamesPlayed
    )
    {
        public static UserDetailsDto FromUser(User user) =>
            new(
                user.UserId,
                user.Username,
                user.Email,
                user.PasswordHash,
                user.AccountType,
                user.SubscriptionEndDate,
                user.TotalWins,
                user.TotalGamesPlayed
            );
    }

    private sealed record UserAddRequest(
        string Username,
        string Email,
        string PasswordHash,
        AccountType AccountType = AccountType.FREE,
        Instant? SubscriptionEndDate = null,
        int TotalWins = 0,
        int TotalGamesPlayed = 0
    );

    private sealed record UserUpdateRequest(
        string Username,
        string Email,
        string PasswordHash,
        AccountType AccountType,
        Instant? SubscriptionEndDate,
        int TotalWins,
        int TotalGamesPlayed
    );
}
