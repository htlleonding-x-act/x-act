using Microsoft.AspNetCore.Mvc;
using XActBackend.Core.Services;
using XActBackend.Util;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace XActBackend.Controllers;

[Authorize]
[Route("api/auth")]
public sealed class AuthController(
    IUserService userService,
    ILogger<AuthController> logger
) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<UserDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<UserDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async ValueTask<IActionResult> GetOrCreateUser()
    {
        // In .NET 6+ MapInboundClaims defaults to false, so use raw JWT claim names.
        var keycloakSubject = User.FindFirst("sub")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(keycloakSubject))
        {
            logger.LogWarning("No Keycloak subject found in JWT claims.");
            return Unauthorized();
        }

        var username = User.FindFirst("preferred_username")?.Value
                       ?? User.FindFirst("name")?.Value
                       ?? User.FindFirst(ClaimTypes.Name)?.Value
                       ?? "Player";
        var email = User.FindFirst("email")?.Value
                    ?? User.FindFirst(ClaimTypes.Email)?.Value
                    ?? string.Empty;

        try
        {
            var result = await userService.GetOrCreateByKeycloakSubjectAsync(keycloakSubject, username, email);

            return result.Match<IActionResult>(
                user => Ok(UserDetailsDto.FromUser(user)),
                _ => Problem("Failed to create user.")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get or create user for Keycloak subject {Subject}", keycloakSubject);
            return Problem();
        }
    }
}
