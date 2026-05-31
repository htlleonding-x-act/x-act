using XActBackend.Core.Services;
using XActBackend.Persistence.Model;

namespace XActBackend.Core.Realtime;

public interface IGameSessionSnapshotService
{
    public ValueTask<GameSessionSnapshot?> BuildSnapshotAsync(int sessionId);
}

public interface IGameSessionRealtimePublisher
{
    public ValueTask PublishTeamAddedAsync(Team team);
    public ValueTask PublishTeamUpdatedAsync(Team team);
    public ValueTask PublishTeamDeletedAsync(int sessionId, int teamId);
    public ValueTask PublishTeamMemberJoinedAsync(TeamMember member);
    public ValueTask PublishTeamMemberUpdatedAsync(TeamMember member);
    public ValueTask PublishTeamMemberLeftAsync(int sessionId, int teamId, int memberId, int? userId, string? guestName, Instant leftAt);
    public ValueTask PublishGameSessionStartedAsync(GameSession gameSession);
    public ValueTask PublishLocationLogRecordedAsync(int sessionId, int teamId, LocationLog log);
    public ValueTask PublishMrXCaughtAsync(Team newMrXTeam, Team formerMrXTeam);
}

internal sealed class GameSessionSnapshotService(
    IGameSessionService gameSessionService,
    ITeamService teamService,
    ITeamMemberService teamMemberService,
    ILocationLogService locationLogService) : IGameSessionSnapshotService
{
    public async ValueTask<GameSessionSnapshot?> BuildSnapshotAsync(int sessionId)
    {
        var sessionResult = await gameSessionService.GetGameSessionByIdAsync(sessionId, tracking: false);
        GameSession? gameSession = sessionResult.Match<GameSession?>(
            session => session,
            _ => null);

        if (gameSession is null)
        {
            return null;
        }

        IReadOnlyCollection<Team> teams = await teamService.GetTeamsBySessionIdAsync(sessionId, tracking: false);

        var members = new List<TeamMember>();
        foreach (var team in teams)
        {
            IReadOnlyCollection<TeamMember> teamMembers =
                await teamMemberService.GetMembersByTeamIdAsync(sessionId, team.Id, tracking: false);

            members.AddRange(teamMembers);
        }

        IReadOnlyCollection<LocationLog> locationLogs = await locationLogService.GetLogsBySessionIdAsync(sessionId, tracking: false);

        var teamRoleByTeamId = teams.ToDictionary(team => team.Id, team => team.Role);
        var isMisterXMemberById = members.ToDictionary(
            member => member.Id,
            member => teamRoleByTeamId.GetValueOrDefault(member.TeamId) == TeamRole.MrX);

        IReadOnlyCollection<SnapshotLatestLocationDto> latestLocations =
        [
            .. locationLogs
                .GroupBy(log => log.MemberId)
                .Select(group =>
                {
                    bool isMisterX = isMisterXMemberById.GetValueOrDefault(group.Key);
                    IEnumerable<LocationLog> candidates = isMisterX
                        ? group.Where(log => log.IsRevealedPosition)
                        : group;

                    return candidates
                        .OrderByDescending(log => log.Timestamp)
                        .ThenByDescending(log => log.Id)
                        .FirstOrDefault();
                })
                .Where(log => log is not null)
                .Select(log => log!)
                .Select(log => new SnapshotLatestLocationDto(
                    log.Id,
                    log.MemberId,
                    log.Timestamp,
                    log.Latitude,
                    log.Longitude,
                    log.AccuracyMeters,
                    log.TransportMode,
                    log.IsRevealedPosition))
        ];

        return new GameSessionSnapshot(
            gameSession.Id,
            gameSession.SessionName,
            gameSession.Status,
            gameSession.StartTime,
            gameSession.EndTime,
            gameSession.PlannedDurationMinutes,
            gameSession.MrXRevealInterval,
            [.. teams.Select(team => new SnapshotTeamDto(
                team.Id,
                team.SessionId,
                team.TeamName,
                team.Role,
                team.ColorCode,
                team.IsCaught,
                team.MaxPlayerCount))],
            [.. members.Select(member => new SnapshotTeamMemberDto(
                member.Id,
                member.SessionId,
                member.TeamId,
                member.UserId,
                member.GuestName,
                member.IsTeamLeader,
                member.CurrentLatitude,
                member.CurrentLongitude,
                member.LastUpdated,
                member.JoinedAt))],
            [.. latestLocations]);
    }
}
