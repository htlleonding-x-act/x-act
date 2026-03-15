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

[Route("api/users")]
public sealed class UserController(
    ITransactionProvider transaction,
    IUserService userService,
    ILogger<UserController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<UserListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<UserListResponse>> GetAllUsers()
    {
        IReadOnlyCollection<User> users = await userService.GetAllUsersAsync(tracking: false);

        return Ok(new UserListResponse
        {
            Items = users.Select(UserInformationDto.FromUser).ToList()
        });
    }

    [HttpGet]
    [Route("{userId:int}")]
    [ProducesResponseType<UserDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<UserDetailsDto>> GetUserById([FromRoute] int userId)
    {
        OneOf<User, NotFound> userResult = await userService.GetUserByIdAsync(userId, tracking: false);

        return userResult.Match<ActionResult<UserDetailsDto>>(
            user => Ok(UserDetailsDto.FromUser(user)),
            notFound => NotFound()
        );
    }

    [HttpPost]
    [Route("")]
    [ProducesResponseType<UserDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddUser([FromBody] UserAddRequest addRequest)
    {
        if (!ValidateRequest<UserAddRequest.Validator, UserAddRequest>(addRequest))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<User, Error> addResult = await userService.AddUserAsync(
                new IUserService.UserData(
                    addRequest.Username,
                    addRequest.Email,
                    addRequest.AccountType,
                    addRequest.SubscriptionEndDate,
                    addRequest.TotalWins,
                    addRequest.TotalGamesPlayed
                )
            );

            return await addResult.Match<ValueTask<IActionResult>>(async user =>
            {
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetUserById), new { userId = user.Id },
                    UserDetailsDto.FromUser(user));
            }, async error =>
            {
                await transaction.RollbackAsync();

                return BadRequest();
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add user");
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpPut]
    [Route("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> UpdateUser(
        [FromRoute] int userId,
        [FromBody] UserUpdateRequest updateRequest)
    {
        if (!ValidateRequest<UserUpdateRequest.Validator, UserUpdateRequest>(updateRequest))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> updateResult = await userService.UpdateUserAsync(
                userId,
                new IUserService.UserData(
                    updateRequest.Username,
                    updateRequest.Email,
                    updateRequest.AccountType,
                    updateRequest.SubscriptionEndDate,
                    updateRequest.TotalWins,
                    updateRequest.TotalGamesPlayed
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
            logger.LogError(ex, "Failed to update user {UserId}", userId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpDelete]
    [Route("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteUser([FromRoute] int userId)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await userService.DeleteUserAsync(userId, tracking: true);

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
            logger.LogError(ex, "Failed to delete user {UserId}", userId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }
}

public sealed class UserListResponse
{
    public required List<UserInformationDto> Items { get; init; }
}

public sealed record UserInformationDto(
    int Id,
    string? Username,
    string? Email,
    AccountType AccountType
)
{
    public static UserInformationDto FromUser(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.AccountType
        );
}

public sealed record UserDetailsDto(
    int Id,
    string? Username,
    string? Email,
    AccountType AccountType,
    Instant? SubscriptionEndDate,
    int TotalWins,
    int TotalGamesPlayed
)
{
    public static UserDetailsDto FromUser(User user) =>
        new(
            user.Id,
            user.Username,
            user.Email,
            user.AccountType,
            user.SubscriptionEndDate,
            user.TotalWins,
            user.TotalGamesPlayed
        );
}

public sealed record UserAddRequest(
    string Username,
    string Email,
    AccountType AccountType = AccountType.Free,
    Instant? SubscriptionEndDate = null,
    int TotalWins = 0,
    int TotalGamesPlayed = 0
)
{
    public sealed class Validator : AbstractValidator<UserAddRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
            RuleFor(x => x.AccountType).IsInEnum();
            RuleFor(x => x.TotalWins).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalGamesPlayed).GreaterThanOrEqualTo(0);
        }
    }
}

public sealed record UserUpdateRequest(
    string Username,
    string Email,
    AccountType AccountType,
    Instant? SubscriptionEndDate,
    int TotalWins,
    int TotalGamesPlayed
)
{
    public sealed class Validator : AbstractValidator<UserUpdateRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(100);
            RuleFor(x => x.AccountType).IsInEnum();
            RuleFor(x => x.TotalWins).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalGamesPlayed).GreaterThanOrEqualTo(0);
        }
    }
}
