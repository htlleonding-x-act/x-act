using Microsoft.AspNetCore.Mvc;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Util;

namespace XActBackend.Controllers;

[Route("api/gamesessions/{sessionId:int}/locationlogs")]
public sealed class GameSessionLocationLogController(
    ILocationLogService locationLogService) : BaseController
{
    [HttpGet]
    [Route("")]
    [ProducesResponseType<LocationLogListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<LocationLogListResponse>> GetAllLocationLogsBySession([FromRoute] int sessionId)
    {
        IReadOnlyCollection<LocationLog> locationLogs = await locationLogService.GetLogsBySessionIdAsync(sessionId, tracking: false);

        return Ok(new LocationLogListResponse
        {
            Items = locationLogs.Select(LocationLogInformationDto.FromLocationLog).ToList()
        });
    }
}
