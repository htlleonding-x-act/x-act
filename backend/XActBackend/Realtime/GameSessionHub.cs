using Microsoft.AspNetCore.SignalR;

namespace XActBackend.Realtime;

public sealed class GameSessionHub(
    IGameSessionSnapshotService snapshotService,
    ILogger<GameSessionHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Realtime client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            logger.LogInformation("Realtime client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            logger.LogWarning(exception, "Realtime client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<GameSessionSnapshot> SubscribeSession(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new HubException("Invalid session id");
        }

        GameSessionSnapshot? snapshot = await snapshotService.BuildSnapshotAsync(sessionId);
        if (snapshot is null)
        {
            throw new HubException("Session not found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Session(sessionId));

        return snapshot;
    }

    public Task UnsubscribeSession(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new HubException("Invalid session id");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Session(sessionId));
    }

    public async Task<GameSessionSnapshot> RequestSnapshot(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new HubException("Invalid session id");
        }

        GameSessionSnapshot? snapshot = await snapshotService.BuildSnapshotAsync(sessionId);
        if (snapshot is null)
        {
            throw new HubException("Session not found");
        }

        return snapshot;
    }
}
