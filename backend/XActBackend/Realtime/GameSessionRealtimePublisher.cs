using Microsoft.AspNetCore.SignalR;
using XActBackend.Core.Realtime;
using XActBackend.Persistence.Model;

namespace XActBackend.Realtime;

internal sealed class GameSessionRealtimePublisher(
    IHubContext<GameSessionHub> hubContext,
    ILogger<GameSessionRealtimePublisher> logger) : IGameSessionRealtimePublisher
{
    public ValueTask PublishTeamAddedAsync(Team team) =>
        PublishToSessionAsync(
            team.SessionId,
            RealtimeEvents.TeamAdded,
            new TeamAddedPayload(
                team.Id,
                team.SessionId,
                team.TeamName,
                team.Role,
                team.ColorCode,
                team.IsCaught,
                team.MaxPlayerCount));

    public ValueTask PublishTeamUpdatedAsync(Team team) =>
        PublishToSessionAsync(
            team.SessionId,
            RealtimeEvents.TeamUpdated,
            new TeamUpdatedPayload(
                team.Id,
                team.SessionId,
                team.TeamName,
                team.Role,
                team.ColorCode,
                team.IsCaught,
                team.MaxPlayerCount));

    public ValueTask PublishTeamDeletedAsync(int sessionId, int teamId) =>
        PublishToSessionAsync(
            sessionId,
            RealtimeEvents.TeamDeleted,
            new TeamDeletedPayload(teamId, sessionId));

    public ValueTask PublishTeamMemberJoinedAsync(TeamMember member) =>
        PublishToSessionAsync(
            member.SessionId,
            RealtimeEvents.TeamMemberJoined,
            new TeamMemberJoinedPayload(
                member.Id,
                member.SessionId,
                member.TeamId,
                member.UserId,
                member.GuestName,
                member.IsTeamLeader,
                member.CurrentLatitude,
                member.CurrentLongitude,
                member.LastUpdated,
                member.JoinedAt));

    public ValueTask PublishTeamMemberUpdatedAsync(TeamMember member) =>
        PublishToSessionAsync(
            member.SessionId,
            RealtimeEvents.TeamMemberUpdated,
            new TeamMemberUpdatedPayload(
                member.Id,
                member.SessionId,
                member.TeamId,
                member.UserId,
                member.GuestName,
                member.IsTeamLeader,
                member.CurrentLatitude,
                member.CurrentLongitude,
                member.LastUpdated));

    public ValueTask PublishTeamMemberLeftAsync(int sessionId, int teamId, int memberId, int? userId, string? guestName, Instant leftAt) =>
        PublishToSessionAsync(
            sessionId,
            RealtimeEvents.TeamMemberLeft,
            new TeamMemberLeftPayload(memberId, sessionId, teamId, userId, guestName, leftAt));

    public ValueTask PublishGameSessionStartedAsync(GameSession gameSession) =>
        PublishToSessionAsync(
            gameSession.Id,
            RealtimeEvents.GameSessionStarted,
            new GameSessionStartedPayload(
                gameSession.Id,
                gameSession.Status,
                gameSession.StartTime,
                gameSession.EndTime));

    public ValueTask PublishLocationLogRecordedAsync(int sessionId, int teamId, LocationLog log) =>
        PublishToSessionAsync(
            sessionId,
            RealtimeEvents.LocationLogRecorded,
            new LocationLogRecordedPayload(
                log.Id,
                sessionId,
                teamId,
                log.MemberId,
                log.Timestamp,
                log.Latitude,
                log.Longitude,
                log.AccuracyMeters,
                log.TransportMode,
                log.IsRevealedPosition));

    public ValueTask PublishMrXCaughtAsync(Team newMrXTeam, Team formerMrXTeam) =>
        PublishToSessionAsync(
            newMrXTeam.SessionId,
            RealtimeEvents.MrXCaught,
            new MrXCaughtPayload(
                newMrXTeam.SessionId,
                newMrXTeam.Id,
                newMrXTeam.TeamName,
                formerMrXTeam.Id,
                formerMrXTeam.TeamName));

    private async ValueTask PublishToSessionAsync(int sessionId, string eventType, object payload)
    {
        try
        {
            await hubContext.Clients
                .Group(RealtimeGroups.Session(sessionId))
                .SendAsync(RealtimeMethods.Event, new RealtimeEventEnvelope(eventType, payload));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish realtime event {EventType} for session {SessionId}", eventType, sessionId);
        }
    }
}
