using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Authorize]
[Route("api/auth")]
public sealed class AuthController(
    IUserService userService,
    ILogger<AuthController> logger) : BaseController
{
    private const string FallbackUsername = "Player";

    /// <summary>links the keycloak identity of the bearer token to a user, creating one on first login</summary>
    [HttpPost]
    [Route("register")]
    [ProducesResponseType<UserDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<ActionResult<UserDetailsDto>> RegisterCurrentUser()
    {
        string? keycloakSubject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrWhiteSpace(keycloakSubject))
        {
            logger.LogWarning("Bearer token without a subject claim reached the register endpoint");

            return Unauthorized();
        }

        string username = User.FindFirst(JwtRegisteredClaimNames.PreferredUsername)?.Value ?? FallbackUsername;
        string email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;

        OneOf<User, Error> userResult =
            await userService.GetOrCreateByKeycloakSubjectAsync(keycloakSubject, username, email);

        return userResult.Match<ActionResult<UserDetailsDto>>(
            user => Ok(UserDetailsDto.FromUser(user)),
            _ => Problem()
        );
    }
}
