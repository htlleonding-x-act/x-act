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
    IPlayerStatsService playerStatsService,
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

    [HttpGet]
    [Route("stats")]
    [ProducesResponseType<PlayerStatsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<ActionResult<PlayerStatsDto>> GetMyStats()
    {
        if (KeycloakSubject is not { } userId)
        {
            return Unauthorized();
        }

        IPlayerStatsService.PlayerStats stats = await playerStatsService.GetStatsAsync(userId);

        return Ok(PlayerStatsDto.FromStats(stats));
    }

    [HttpGet]
    [Route("matches")]
    [ProducesResponseType<MatchHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<ActionResult<MatchHistoryResponse>> GetMyMatches([FromQuery] MatchHistoryQuery query)
    {
        if (KeycloakSubject is not { } userId)
        {
            return Unauthorized();
        }

        if (!ValidateRequest<MatchHistoryQuery.Validator, MatchHistoryQuery>(query))
        {
            return BadRequest();
        }

        IReadOnlyList<IPlayerStatsService.MatchSummary> matches =
            await playerStatsService.GetMatchHistoryAsync(userId, query.Limit);

        return Ok(new MatchHistoryResponse
        {
            Items = matches.Select(MatchSummaryDto.FromSummary).ToList()
        });
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
                    updateRequest.AvatarIcon
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
    string? AvatarIcon
)
{
    public static MyProfileDto FromUser(User user) =>
        new(
            user.Id,
            user.Username!,
            user.Email,
            user.AccountType,
            user.CreatedAt,
            user.AvatarIcon
        );
}

/// <summary>replaces the whole profile; a null avatar icon falls back to the initials</summary>
public sealed record UpdateMyProfileRequest(string Username, string? AvatarIcon = null)
{
    public sealed class Validator : AbstractValidator<UpdateMyProfileRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
            // only the shape of a key and the column length of User.AvatarIcon. the app owns the
            // icon set and shows initials for a key it doesn't know
            RuleFor(x => x.AvatarIcon).Matches("^[a-z_]{1,32}$").When(x => x.AvatarIcon is not null);
        }
    }
}

public sealed record PlayerStatsDto(
    int GamesPlayed,
    int Wins,
    double DistanceMeters,
    double MrXSeconds,
    int CatchesMade,
    int PowerUpsUsed,
    double TopSpeedKmh
)
{
    public static PlayerStatsDto FromStats(IPlayerStatsService.PlayerStats stats) =>
        new(
            stats.GamesPlayed,
            stats.Wins,
            stats.DistanceMeters,
            stats.MrXSeconds,
            stats.CatchesMade,
            stats.PowerUpsUsed,
            stats.TopSpeedKmh
        );
}

public sealed record MatchHistoryQuery(int Limit = MatchHistoryQuery.DefaultLimit)
{
    public const int DefaultLimit = 20;

    // every match loads its full results, so a page stays small
    public const int MaxLimit = 50;

    public sealed class Validator : AbstractValidator<MatchHistoryQuery>
    {
        public Validator()
        {
            RuleFor(x => x.Limit).InclusiveBetween(1, MaxLimit);
        }
    }
}

public sealed class MatchHistoryResponse
{
    public required List<MatchSummaryDto> Items { get; init; }
}

public sealed record MatchSummaryDto(
    int SessionId,
    string SessionName,
    Instant StartTime,
    Instant EndTime,
    int MemberId,
    string TeamName,
    bool Won,
    double DistanceMeters,
    double MrXSeconds,
    int CatchesMade
)
{
    public static MatchSummaryDto FromSummary(IPlayerStatsService.MatchSummary summary) =>
        new(
            summary.SessionId,
            summary.SessionName,
            summary.Start,
            summary.End,
            summary.MemberId,
            summary.TeamName,
            summary.Won,
            summary.Stats.DistanceMeters,
            summary.Stats.MrXSeconds,
            summary.Stats.CatchesMade
        );
}
