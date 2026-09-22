using Microsoft.AspNetCore.SignalR;
using OneOf;
using OneOf.Types;
using System.Collections.Concurrent;
using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;

namespace XActBackend.Realtime;

public sealed class GameSessionHub(
    ITeamMemberService teamMemberService,
    IGameSessionSnapshotService snapshotService,
    ILobbyDisconnectCleanup lobbyDisconnectCleanup,
    ILogger<GameSessionHub> logger) : Hub
{
    private static readonly ConcurrentDictionary<string, MemberPresenceRegistration> presenceByConnection = new();

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Realtime client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (presenceByConnection.TryRemove(Context.ConnectionId, out MemberPresenceRegistration? registration))
        {
            lobbyDisconnectCleanup.ScheduleRemoval(registration);
        }

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

    public Task RegisterMemberPresence(int sessionId, int teamId, int memberId, int? userId = null, string? guestName = null)
    {
        if (sessionId <= 0 || teamId <= 0 || memberId <= 0)
        {
            throw new HubException("Invalid member presence payload");
        }

        int? normalizedUserId = userId.HasValue && userId.Value > 0 ? userId : null;
        string? normalizedGuestName = string.IsNullOrWhiteSpace(guestName) ? null : guestName;

        presenceByConnection[Context.ConnectionId] = new MemberPresenceRegistration(
            sessionId,
            teamId,
            memberId,
            normalizedUserId,
            normalizedGuestName);

        return Task.CompletedTask;
    }

    public Task UnregisterMemberPresence()
    {
        presenceByConnection.TryRemove(Context.ConnectionId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     members with a live connection in the session. the rematch and the lobby cleanup use this to tell who
    ///     is still around
    /// </summary>
    public static IReadOnlySet<int> GetConnectedMemberIds(int sessionId) =>
        presenceByConnection.Values
            .Where(registration => registration.SessionId == sessionId)
            .Select(registration => registration.MemberId)
            .ToHashSet();

    public async Task<GameSessionSnapshot> SubscribeSession(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new HubException("Invalid session id");
        }

        GameSessionSnapshot snapshot = await BuildSnapshotOrThrowAsync(sessionId);

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

    public async Task JoinTeamChannel(int sessionId, int teamId)
    {
        if (sessionId <= 0 || teamId <= 0)
        {
            throw new HubException("Invalid team channel id");
        }

        // a team channel only exists for a team that has members in this session
        IReadOnlyCollection<TeamMember> teamMembers = await teamMemberService.GetMembersByTeamIdAsync(sessionId, teamId, tracking: false);
        if (teamMembers.Count == 0)
        {
            throw new HubException("Team channel not found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Team(sessionId, teamId));
    }

    public Task LeaveTeamChannel(int sessionId, int teamId)
    {
        if (sessionId <= 0 || teamId <= 0)
        {
            throw new HubException("Invalid team channel id");
        }

        return Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Team(sessionId, teamId));
    }

    public async Task<GameSessionSnapshot> RequestSnapshot(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new HubException("Invalid session id");
        }

        return await BuildSnapshotOrThrowAsync(sessionId);
    }

    private async ValueTask<GameSessionSnapshot> BuildSnapshotOrThrowAsync(int sessionId)
    {
        OneOf<GameSessionSnapshot, NotFound> snapshotResult = await snapshotService.BuildSnapshotAsync(sessionId);

        return snapshotResult.Match(
            snapshot => snapshot,
            _ => throw new HubException("Session not found"));
    }
}
