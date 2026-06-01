using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;
using XActBackend.Core.Util;
using XActBackend.Core.Services;
using XActBackend.Core.Realtime;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;
using XActBackend.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using System.Security.Claims;

namespace XActBackend.Controllers;

[Authorize]
[Route("api/auth")]
public sealed class AuthController(
   ITransactionProvider transaction,
   ILogger<AuthController> logger,
   IUserService userService
) : ControllerBase
{
    
    
      [HttpPost]
      [Route("register")]
      [ProducesResponseType(StatusCodes.Status201Created)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async ValueTask<IActionResult> GetOrCreateUser([FromBody] RegisterRequest request)
      {
         var keycloakId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

         if (string.IsNullOrEmpty(keycloakId))
         {
            logger.LogWarning("No Keycloak ID found in claims for user registration.");
            return Unauthorized();
         }

         try
         {
            var result = await userService.GetUserByIdAsync(keycloakId, false);

            return await result.Match<ValueTask<IActionResult>>(async user =>
            {
               return Ok(UserDetailsDto.FromUser(user));
            }, async _ =>
            {
               await transaction.BeginTransactionAsync();

               var newPlayer = new IUserService.UserData(
                  User.FindFirst(ClaimTypes.Name)?.Value ?? "Neuer Spieler",
                  User.FindFirst(ClaimTypes.Email)?.Value ?? ""
               );

               var addResult = await userService.AddUserAsync(newPlayer);

               return await addResult.Match<ValueTask<IActionResult>>(async createdUser =>
               {
                  await transaction.CommitAsync();
                        return CreatedAtAction("GetUserById", "User", new { userId = createdUser.Id }, UserDetailsDto.FromUser(createdUser));
               }, async _ =>
                {
                   await transaction.RollbackAsync();
                   return BadRequest();
                });
            });
         }
         catch (Exception ex)
         {
            logger.LogError(ex, "Failed to get or create user for Keycloak ID {KeycloakId}", keycloakId);
            try
            {
               await transaction.RollbackAsync();
            }
            catch (Exception rEx)
            {
               logger.LogWarning(rEx, "Rollback failed after GetOrCreateUser error");
            }

            return Problem();
         }

      }

      [HttpPost]
      [Route("login")]
      [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async ValueTask<IActionResult> Login([FromBody] LoginRequest request)
   {
         throw new NotImplementedException();
   }
}

