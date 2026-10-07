using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;

namespace XActBackend.Controllers;

/// <summary>the signed-in user's own profile; the user id comes from the bearer token, never from the request</summary>
[Authorize]
[Route("api/users/me")]
public sealed class MeController(
    ITransactionProvider transaction,
    IUserService userService,
    ILogger<MeController> logger) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<MyProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<MyProfileDto>> GetMyProfile()
    {
        if (KeycloakSubject is not { } userId)
        {
            return Unauthorized();
        }

        OneOf<User, NotFound> userResult = await userService.GetUserByIdAsync(userId, tracking: false);

        return userResult.Match<ActionResult<MyProfileDto>>(
            user => Ok(MyProfileDto.FromUser(user)),
            notFound => NotFound()
        );
    }

    [HttpPut]
    [Route("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest updateRequest)
    {
        if (KeycloakSubject is not { } userId)
        {
            return Unauthorized();
        }

        if (!ValidateRequest<UpdateMyProfileRequest.Validator, UpdateMyProfileRequest>(updateRequest))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound, DomainError> updateResult = await userService.UpdateProfileAsync(
                userId,
                new IUserService.ProfileData(
                    updateRequest.Username.Trim(),
                    updateRequest.AvatarEmoji,
                    updateRequest.AvatarColor?.ToUpperInvariant()
                )
            );

            return await updateResult.Match<ValueTask<IActionResult>>(async success =>
            {
                await transaction.CommitAsync();

                return NoContent();
            }, async notFound =>
            {
                await transaction.RollbackAsync();

                return NotFound();
            }, async domainError =>
            {
                await transaction.RollbackAsync();

                return DomainErrorResult(domainError);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update profile of user {UserId}", userId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }

    [HttpDelete]
    [Route("")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> DeleteMyAccount()
    {
        if (KeycloakSubject is not { } userId)
        {
            return Unauthorized();
        }

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
            logger.LogError(ex, "Failed to delete account of user {UserId}", userId);
            await transaction.RollbackAsync();

            return Problem();
        }
    }
}

public sealed record MyProfileDto(
    string Id,
    string Username,
    string? Email,
    AccountType AccountType,
    Instant CreatedAt,
    string? AvatarEmoji,
    string? AvatarColor
)
{
    public static MyProfileDto FromUser(User user) =>
        new(
            user.Id,
            user.Username!,
            user.Email,
            user.AccountType,
            user.CreatedAt,
            user.AvatarEmoji,
            user.AvatarColor
        );
}

/// <summary>replaces the whole profile; a null avatar field falls back to the default look</summary>
public sealed record UpdateMyProfileRequest(string Username, string? AvatarEmoji = null, string? AvatarColor = null)
{
    public sealed class Validator : AbstractValidator<UpdateMyProfileRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            // column lengths of User.AvatarEmoji and User.AvatarColor
            RuleFor(x => x.AvatarEmoji).NotEmpty().MaximumLength(16).When(x => x.AvatarEmoji is not null);
            RuleFor(x => x.AvatarColor).Matches("^#[0-9A-Fa-f]{6}$").When(x => x.AvatarColor is not null);
        }
    }
}
