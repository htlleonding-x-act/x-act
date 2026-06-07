using Microsoft.AspNetCore.SignalR;
using OneOf;
using OneOf.Types;
using System.Collections.Concurrent;
using XActBackend.Core.Realtime;
using XActBackend.Core.Services;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Realtime;

public sealed class GameSessionHub(
    ITransactionProvider transaction,
    IGameSessionService gameSessionService,
    ITeamMemberService teamMemberService,
    IGameSessionRealtimePublisher realtimePublisher,
    IClock clock,
    IGameSessionSnapshotService snapshotService,
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
        await RemoveDisconnectedLobbyMemberAsync();

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

    public Task RegisterMemberPresence(int sessionId, int teamId, int memberId, string? userId = null, string? guestName = null)
    {
        if (sessionId <= 0 || teamId <= 0 || memberId <= 0)
        {
            throw new HubException("Invalid member presence payload");
        }

        string? normalizedUserId = string.IsNullOrWhiteSpace(userId) ? null : userId;
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

    public async Task JoinTeamChannel(int sessionId, int teamId)
    {
        if (sessionId <= 0 || teamId <= 0)
        {
            throw new HubException("Invalid team channel id");
        }

        // Only allow joining a team channel that actually exists in the session and has members.
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

        GameSessionSnapshot? snapshot = await snapshotService.BuildSnapshotAsync(sessionId);
        if (snapshot is null)
        {
            throw new HubException("Session not found");
        }

        return snapshot;
    }

    private async Task RemoveDisconnectedLobbyMemberAsync()
    {
        if (!presenceByConnection.TryRemove(Context.ConnectionId, out MemberPresenceRegistration? registration))
        {
            return;
        }

        try
        {
            OneOf<GameSession, NotFound> sessionResult = await gameSessionService.GetGameSessionByIdAsync(registration.SessionId, tracking: false);

            bool isWaiting = sessionResult.Match(
                session => session.Status == SessionStatus.Waiting,
                _ => false);

            if (!isWaiting)
            {
                return;
            }

            await transaction.BeginTransactionAsync();

            OneOf<Success, NotFound> deleteResult = await teamMemberService.DeleteTeamMemberAsync(
                registration.SessionId,
                registration.TeamId,
                registration.MemberId,
                tracking: true);

            await deleteResult.Match(
                async _ =>
                {
                    await transaction.CommitAsync();
                    await realtimePublisher.PublishTeamMemberLeftAsync(
                        registration.SessionId,
                        registration.TeamId,
                        registration.MemberId,
                        registration.UserId,
                        registration.GuestName,
                        clock.GetCurrentInstant());
                },
                async _ => { await transaction.RollbackAsync(); });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed disconnect cleanup for connection {ConnectionId}", Context.ConnectionId);
            await transaction.RollbackAsync();
        }
    }

    private sealed record MemberPresenceRegistration(
        int SessionId,
        int TeamId,
        int MemberId,
        string? UserId,
        string? GuestName);
}
