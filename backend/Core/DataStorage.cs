using XAct.Core.Users;
using XAct.Core.GameSessions;
using XAct.Core.GeofencePoints;
using XAct.Core.Teams;
using XAct.Core.TeamMembers;
using XAct.Core.LocationLogs;
using XAct.Core.PowerUpUsages;

namespace XAct.Core;

public interface IDataStorage
{
    public ValueTask<IEnumerable<User>> GetUsersAsync();
    public ValueTask AddUserAsync(User user);
    public ValueTask RemoveUserAsync(User user);

    public ValueTask<IEnumerable<GameSession>> GetGameSessionsAsync();
    public ValueTask AddGameSessionAsync(GameSession gameSession);
    public ValueTask RemoveGameSessionAsync(GameSession gameSession);

    public ValueTask<IEnumerable<GeofencePoint>> GetGeofencePointsAsync();
    public ValueTask AddGeofencePointAsync(GeofencePoint geofencePoint);
    public ValueTask RemoveGeofencePointAsync(GeofencePoint geofencePoint);

    public ValueTask<IEnumerable<Team>> GetTeamsAsync();
    public ValueTask AddTeamAsync(Team team);
    public ValueTask RemoveTeamAsync(Team team);

    public ValueTask<IEnumerable<TeamMember>> GetTeamMembersAsync();
    public ValueTask AddTeamMemberAsync(TeamMember teamMember);
    public ValueTask RemoveTeamMemberAsync(TeamMember teamMember);

    public ValueTask<IEnumerable<LocationLog>> GetLocationLogsAsync();
    public ValueTask AddLocationLogAsync(LocationLog locationLog);
    public ValueTask RemoveLocationLogAsync(LocationLog locationLog);

    public ValueTask<IEnumerable<PowerUpUsage>> GetPowerUpUsagesAsync();
    public ValueTask AddPowerUpUsageAsync(PowerUpUsage powerUpUsage);
    public ValueTask RemovePowerUpUsageAsync(PowerUpUsage powerUpUsage);
}

public sealed class DataStorage : IDataStorage
{
    public ValueTask<IEnumerable<User>> GetUsersAsync() => ValueTask.FromResult(_users.AsEnumerable());

    public ValueTask AddUserAsync(User user)
    {
        _users.Add(user);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveUserAsync(User user)
    {
        _users.Remove(user);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<GameSession>> GetGameSessionsAsync() => ValueTask.FromResult(_gameSessions.AsEnumerable());

    public ValueTask AddGameSessionAsync(GameSession gameSession)
    {
        _gameSessions.Add(gameSession);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveGameSessionAsync(GameSession gameSession)
    {
        _gameSessions.Remove(gameSession);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<GeofencePoint>> GetGeofencePointsAsync() => ValueTask.FromResult(_geofencePoints.AsEnumerable());

    public ValueTask AddGeofencePointAsync(GeofencePoint geofencePoint)
    {
        _geofencePoints.Add(geofencePoint);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveGeofencePointAsync(GeofencePoint geofencePoint)
    {
        _geofencePoints.Remove(geofencePoint);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<Team>> GetTeamsAsync() => ValueTask.FromResult(_teams.AsEnumerable());

    public ValueTask AddTeamAsync(Team team)
    {
        _teams.Add(team);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveTeamAsync(Team team)
    {
        _teams.Remove(team);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<TeamMember>> GetTeamMembersAsync() => ValueTask.FromResult(_teamMembers.AsEnumerable());

    public ValueTask AddTeamMemberAsync(TeamMember teamMember)
    {
        _teamMembers.Add(teamMember);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveTeamMemberAsync(TeamMember teamMember)
    {
        _teamMembers.Remove(teamMember);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<LocationLog>> GetLocationLogsAsync() => ValueTask.FromResult(_locationLogs.AsEnumerable());

    public ValueTask AddLocationLogAsync(LocationLog locationLog)
    {
        _locationLogs.Add(locationLog);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveLocationLogAsync(LocationLog locationLog)
    {
        _locationLogs.Remove(locationLog);
        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<PowerUpUsage>> GetPowerUpUsagesAsync() => ValueTask.FromResult(_powerUpUsages.AsEnumerable());

    public ValueTask AddPowerUpUsageAsync(PowerUpUsage powerUpUsage)
    {
        _powerUpUsages.Add(powerUpUsage);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemovePowerUpUsageAsync(PowerUpUsage powerUpUsage)
    {
        _powerUpUsages.Remove(powerUpUsage);
        return ValueTask.CompletedTask;
    }

    #region sample data
    private readonly List<User> _users = [];
    private readonly List<GameSession> _gameSessions = [];
    private readonly List<GeofencePoint> _geofencePoints = [];
    private readonly List<Team> _teams = [];
    private readonly List<TeamMember> _teamMembers = [];
    private readonly List<LocationLog> _locationLogs = [];
    private readonly List<PowerUpUsage> _powerUpUsages = [];
    #endregion
}
