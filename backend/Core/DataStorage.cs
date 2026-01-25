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
    private readonly List<User> _users =
    [
        new User
        {
            UserId = 1,
            Username = "alice_detective",
            Email = "alice@example.com",
            PasswordHash = "hash_alice_123",
            AccountType = AccountType.PRO,
            SubscriptionEndDate = Instant.FromUtc(2026, 6, 14, 0, 0),
            TotalWins = 12,
            TotalGamesPlayed = 45
        },
        new User
        {
            UserId = 2,
            Username = "bob_runner",
            Email = "bob@example.com",
            PasswordHash = "hash_bob_456",
            AccountType = AccountType.FREE,
            SubscriptionEndDate = null,
            TotalWins = 8,
            TotalGamesPlayed = 30
        },
        new User
        {
            UserId = 3,
            Username = "charlie_host",
            Email = "charlie@example.com",
            PasswordHash = "hash_charlie_789",
            AccountType = AccountType.PRO,
            SubscriptionEndDate = Instant.FromUtc(2026, 12, 31, 0, 0),
            TotalWins = 25,
            TotalGamesPlayed = 60
        },
        new User
        {
            UserId = 4,
            Username = "diana_tracker",
            Email = "diana@example.com",
            PasswordHash = "hash_diana_012",
            AccountType = AccountType.EVENT_PASS,
            SubscriptionEndDate = Instant.FromUtc(2026, 1, 15, 0, 0),
            TotalWins = 5,
            TotalGamesPlayed = 18
        },
        new User
        {
            UserId = 5,
            Username = "eve_mystery",
            Email = "eve@example.com",
            PasswordHash = "hash_eve_345",
            AccountType = AccountType.PRO,
            SubscriptionEndDate = Instant.FromUtc(2026, 3, 20, 0, 0),
            TotalWins = 18,
            TotalGamesPlayed = 42
        }
    ];

    private readonly List<GameSession> _gameSessions =
    [
        new GameSession
        {
            SessionId = 1,
            HostUserId = 3,
            JoinCode = "ABC123",
            Status = SessionStatus.ACTIVE,
            StartTime = Instant.FromUtc(2025, 12, 14, 10, 0),
            EndTime = null,
            PlannedDurationMinutes = 120,
            MrXRevealInterval = 15
        },
        new GameSession
        {
            SessionId = 2,
            HostUserId = 1,
            JoinCode = "XYZ789",
            Status = SessionStatus.FINISHED,
            StartTime = Instant.FromUtc(2025, 12, 13, 14, 30),
            EndTime = Instant.FromUtc(2025, 12, 13, 16, 45),
            PlannedDurationMinutes = 90,
            MrXRevealInterval = 10
        }
    ];

    private readonly List<GeofencePoint> _geofencePoints =
    [
        new GeofencePoint
        {
            PointId = 1,
            SessionId = 1,
            Latitude = 52.5200,
            Longitude = 13.4050,
            SequenceOrder = 1
        },
        new GeofencePoint
        {
            PointId = 2,
            SessionId = 1,
            Latitude = 52.5300,
            Longitude = 13.4050,
            SequenceOrder = 2
        },
        new GeofencePoint
        {
            PointId = 3,
            SessionId = 1,
            Latitude = 52.5300,
            Longitude = 13.3800,
            SequenceOrder = 3
        },
        new GeofencePoint
        {
            PointId = 4,
            SessionId = 1,
            Latitude = 52.5200,
            Longitude = 13.3800,
            SequenceOrder = 4
        },
        new GeofencePoint
        {
            PointId = 5,
            SessionId = 2,
            Latitude = 51.5074,
            Longitude = -0.1278,
            SequenceOrder = 1
        },
        new GeofencePoint
        {
            PointId = 6,
            SessionId = 2,
            Latitude = 51.5174,
            Longitude = -0.1278,
            SequenceOrder = 2
        },
        new GeofencePoint
        {
            PointId = 7,
            SessionId = 2,
            Latitude = 51.5174,
            Longitude = -0.1400,
            SequenceOrder = 3
        },
        new GeofencePoint
        {
            PointId = 8,
            SessionId = 2,
            Latitude = 51.5074,
            Longitude = -0.1400,
            SequenceOrder = 4
        }
    ];

    private readonly List<Team> _teams =
    [
        new Team
        {
            TeamId = 1,
            SessionId = 1,
            TeamName = "Mr. X",
            Role = TeamRole.MR_X,
            ColorCode = "#000000",
            IsCaught = false
        },
        new Team
        {
            TeamId = 2,
            SessionId = 1,
            TeamName = "Blue Detectives",
            Role = TeamRole.DETECTIVE,
            ColorCode = "#0000FF",
            IsCaught = false
        },
        new Team
        {
            TeamId = 3,
            SessionId = 1,
            TeamName = "Red Hunters",
            Role = TeamRole.DETECTIVE,
            ColorCode = "#FF0000",
            IsCaught = false
        },
        new Team
        {
            TeamId = 4,
            SessionId = 2,
            TeamName = "Mr. X",
            Role = TeamRole.MR_X,
            ColorCode = "#000000",
            IsCaught = true
        },
        new Team
        {
            TeamId = 5,
            SessionId = 2,
            TeamName = "Green Squad",
            Role = TeamRole.DETECTIVE,
            ColorCode = "#00FF00",
            IsCaught = false
        }
    ];

    private readonly List<TeamMember> _teamMembers =
    [
        new TeamMember
        {
            MemberId = 1,
            TeamId = 1,
            UserId = 5,
            IsTeamLeader = true,
            CurrentLatitude = 52.5250,
            CurrentLongitude = 13.3950,
            LastUpdated = Instant.FromUtc(2025, 12, 14, 11, 30)
        },
        new TeamMember
        {
            MemberId = 2,
            TeamId = 2,
            UserId = 1,
            IsTeamLeader = true,
            CurrentLatitude = 52.5210,
            CurrentLongitude = 13.4000,
            LastUpdated = Instant.FromUtc(2025, 12, 14, 11, 32)
        },
        new TeamMember
        {
            MemberId = 3,
            TeamId = 2,
            UserId = 4,
            IsTeamLeader = false,
            CurrentLatitude = 52.5215,
            CurrentLongitude = 13.3995,
            LastUpdated = Instant.FromUtc(2025, 12, 14, 11, 31)
        },
        new TeamMember
        {
            MemberId = 4,
            TeamId = 3,
            UserId = 2,
            IsTeamLeader = true,
            CurrentLatitude = 52.5280,
            CurrentLongitude = 13.3850,
            LastUpdated = Instant.FromUtc(2025, 12, 14, 11, 29)
        },
        new TeamMember
        {
            MemberId = 5,
            TeamId = 4,
            UserId = 2,
            IsTeamLeader = true,
            CurrentLatitude = 51.5120,
            CurrentLongitude = -0.1320,
            LastUpdated = Instant.FromUtc(2025, 12, 13, 16, 45)
        },
        new TeamMember
        {
            MemberId = 6,
            TeamId = 5,
            UserId = 4,
            IsTeamLeader = true,
            CurrentLatitude = 51.5115,
            CurrentLongitude = -0.1325,
            LastUpdated = Instant.FromUtc(2025, 12, 13, 16, 45)
        }
    ];

    private readonly List<LocationLog> _locationLogs =
    [
        new LocationLog
        {
            LogId = 1,
            MemberId = 1,
            Timestamp = Instant.FromUtc(2025, 12, 14, 10, 15),
            Latitude = 52.5200,
            Longitude = 13.4000,
            AccuracyMeters = 5.2,
            TransportMode = TransportMode.FOOT,
            IsRevealedPosition = true
        },
        new LocationLog
        {
            LogId = 2,
            MemberId = 1,
            Timestamp = Instant.FromUtc(2025, 12, 14, 10, 30),
            Latitude = 52.5220,
            Longitude = 13.3980,
            AccuracyMeters = 4.8,
            TransportMode = TransportMode.BUS,
            IsRevealedPosition = false
        },
        new LocationLog
        {
            LogId = 3,
            MemberId = 1,
            Timestamp = Instant.FromUtc(2025, 12, 14, 11, 0),
            Latitude = 52.5240,
            Longitude = 13.3960,
            AccuracyMeters = 6.1,
            TransportMode = TransportMode.TRAM,
            IsRevealedPosition = true
        },
        new LocationLog
        {
            LogId = 4,
            MemberId = 1,
            Timestamp = Instant.FromUtc(2025, 12, 14, 11, 30),
            Latitude = 52.5250,
            Longitude = 13.3950,
            AccuracyMeters = 5.5,
            TransportMode = TransportMode.FOOT,
            IsRevealedPosition = false
        },
        new LocationLog
        {
            LogId = 5,
            MemberId = 2,
            Timestamp = Instant.FromUtc(2025, 12, 14, 10, 20),
            Latitude = 52.5205,
            Longitude = 13.4020,
            AccuracyMeters = 3.2,
            TransportMode = TransportMode.FOOT,
            IsRevealedPosition = false
        },
        new LocationLog
        {
            LogId = 6,
            MemberId = 2,
            Timestamp = Instant.FromUtc(2025, 12, 14, 11, 0),
            Latitude = 52.5210,
            Longitude = 13.4005,
            AccuracyMeters = 4.1,
            TransportMode = TransportMode.FOOT,
            IsRevealedPosition = false
        },
        new LocationLog
        {
            LogId = 7,
            MemberId = 2,
            Timestamp = Instant.FromUtc(2025, 12, 14, 11, 32),
            Latitude = 52.5210,
            Longitude = 13.4000,
            AccuracyMeters = 3.8,
            TransportMode = TransportMode.FOOT,
            IsRevealedPosition = false
        },
        new LocationLog
        {
            LogId = 8,
            MemberId = 5,
            Timestamp = Instant.FromUtc(2025, 12, 13, 15, 30),
            Latitude = 51.5100,
            Longitude = -0.1300,
            AccuracyMeters = 4.5,
            TransportMode = TransportMode.TRAIN,
            IsRevealedPosition = true
        },
        new LocationLog
        {
            LogId = 9,
            MemberId = 5,
            Timestamp = Instant.FromUtc(2025, 12, 13, 16, 15),
            Latitude = 51.5120,
            Longitude = -0.1320,
            AccuracyMeters = 5.2,
            TransportMode = TransportMode.FOOT,
            IsRevealedPosition = false
        }
    ];

    private readonly List<PowerUpUsage> _powerUpUsages =
    [
        new PowerUpUsage
        {
            UsageId = 1,
            MemberId = 1,
            PowerUpType = PowerUpType.BLACK_TICKET,
            UsedAt = Instant.FromUtc(2025, 12, 14, 10, 45)
        },
        new PowerUpUsage
        {
            UsageId = 2,
            MemberId = 1,
            PowerUpType = PowerUpType.DOUBLE_MOVE,
            UsedAt = Instant.FromUtc(2025, 12, 14, 11, 15)
        },
        new PowerUpUsage
        {
            UsageId = 3,
            MemberId = 5,
            PowerUpType = PowerUpType.BLACK_TICKET,
            UsedAt = Instant.FromUtc(2025, 12, 13, 15, 45)
        }
    ];
    #endregion
}
