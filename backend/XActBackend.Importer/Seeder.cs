using Microsoft.EntityFrameworkCore;
using NodaTime;
using XActBackend.Persistence.Model;
using XActBackend.Persistence.Util;

namespace XActBackend.Importer;

internal static class Seeder
{
    internal static async Task<bool> InsertInitialSeedData(DatabaseContext ctx)
    {
        await ctx.Database.BeginTransactionAsync();

        if (await ctx.Users.AnyAsync() || await ctx.GameSessions.AnyAsync())
        {
            await ctx.Database.RollbackTransactionAsync();
            return false;
        }

        InsertUsers(ctx);
        InsertGameSessions(ctx);
        InsertTeams(ctx);
        InsertTeamMembers(ctx);
        InsertGeofencePoints(ctx);
        InsertLocationLogs(ctx);
        InsertPowerUpUsages(ctx);

        await ctx.SaveChangesAsync();
        await SyncIdentitySequencesAsync(ctx);
        await ctx.Database.CommitTransactionAsync();

        return true;
    }

    private static async Task SyncIdentitySequencesAsync(DatabaseContext ctx)
    {
        // Seed data sets explicit IDs, so align identity sequences to avoid duplicate key errors on inserts.
        string[] tableNames =
        [
            "User",
            "GameSession",
            "Team",
            "TeamMember",
            "GeofencePoint",
            "LocationLog",
            "PowerUpUsage"
        ];

        foreach (string tableName in tableNames)
        {
            string escapedTableName = tableName.Replace("\"", "\"\"");
            string sql =
                $"""
                 SELECT setval(
                     pg_get_serial_sequence('"{DatabaseContext.SchemaName}"."{escapedTableName}"', 'Id'),
                     COALESCE((SELECT MAX("Id") FROM "{DatabaseContext.SchemaName}"."{escapedTableName}"), 1),
                     true
                 );
                 """;

            await ctx.Database.ExecuteSqlRawAsync(sql);
        }
    }

    private static void InsertUsers(DatabaseContext ctx)
    {
        ctx.Users.AddRange(
            new User
            {
                Id = SeedData.HostUserId,
                Username = "host_user",
                Email = "host@example.com",
                AccountType = AccountType.Free,
                TotalWins = 1,
                TotalGamesPlayed = 2,
                CreatedAt = SeedData.BaseInstant
            },
            new User
            {
                Id = SeedData.DetectiveUserId,
                Username = "detective_user",
                Email = "detective@example.com",
                AccountType = AccountType.Pro,
                TotalWins = 3,
                TotalGamesPlayed = 5,
                CreatedAt = SeedData.BaseInstant
            },
            new User
            {
                Id = SeedData.SpectatorUserId,
                Username = "spectator_user",
                Email = "spectator@example.com",
                AccountType = AccountType.Free,
                TotalWins = 0,
                TotalGamesPlayed = 1,
                CreatedAt = SeedData.BaseInstant
            }
        );
    }

    private static void InsertGameSessions(DatabaseContext ctx)
    {
        ctx.GameSessions.AddRange(
            new GameSession
            {
                Id = SeedData.SessionId,
                HostUserId = SeedData.HostUserId,
                SessionName = "Alpha Session",
                JoinCode = SeedData.SessionJoinCode,
                Status = SessionStatus.Waiting,
                StartTime = SeedData.BaseInstant,
                EndTime = SeedData.BaseInstant.Plus(Duration.FromHours(2)),
                PlannedDurationMinutes = 120,
                MrXRevealInterval = 5,
                CreatedAt = SeedData.BaseInstant
            },
            new GameSession
            {
                Id = SeedData.SessionTwoId,
                HostUserId = SeedData.DetectiveUserId,
                SessionName = "Bravo Session",
                JoinCode = SeedData.SessionTwoJoinCode,
                Status = SessionStatus.Active,
                StartTime = SeedData.BaseInstant.Plus(Duration.FromHours(3)),
                EndTime = null,
                PlannedDurationMinutes = 90,
                MrXRevealInterval = 6,
                CreatedAt = SeedData.BaseInstant
            }
        );
    }

    private static void InsertTeams(DatabaseContext ctx)
    {
        ctx.Teams.AddRange(
            new Team
            {
                Id = SeedData.MrXTeamId,
                SessionId = SeedData.SessionId,
                TeamName = "MrX",
                Role = TeamRole.MrX,
                ColorCode = "#000000",
                IsCaught = false
            },
            new Team
            {
                Id = SeedData.DetectiveTeamId,
                SessionId = SeedData.SessionId,
                TeamName = "Detectives",
                Role = TeamRole.Detective,
                ColorCode = "#ff0000",
                IsCaught = false
            },
            new Team
            {
                Id = SeedData.SessionTwoTeamId,
                SessionId = SeedData.SessionTwoId,
                TeamName = "Bravo Team",
                Role = TeamRole.Detective,
                ColorCode = "#00ff00",
                IsCaught = false
            }
        );
    }

    private static void InsertTeamMembers(DatabaseContext ctx)
    {
        ctx.TeamMembers.AddRange(
            new TeamMember
            {
                Id = SeedData.HostMemberId,
                SessionId = SeedData.SessionId,
                TeamId = SeedData.MrXTeamId,
                UserId = SeedData.HostUserId,
                GuestName = null,
                IsTeamLeader = true,
                JoinedAt = SeedData.BaseInstant,
                CurrentLatitude = 48.2,
                CurrentLongitude = 16.3,
                LastUpdated = SeedData.BaseInstant
            },
            new TeamMember
            {
                Id = SeedData.DetectiveMemberId,
                SessionId = SeedData.SessionId,
                TeamId = SeedData.DetectiveTeamId,
                UserId = SeedData.DetectiveUserId,
                GuestName = null,
                IsTeamLeader = false,
                JoinedAt = SeedData.BaseInstant,
                CurrentLatitude = 48.21,
                CurrentLongitude = 16.31,
                LastUpdated = SeedData.BaseInstant.Plus(Duration.FromMinutes(5))
            },
            new TeamMember
            {
                Id = SeedData.GuestMemberId,
                SessionId = SeedData.SessionId,
                TeamId = SeedData.DetectiveTeamId,
                UserId = null,
                GuestName = "Guest A",
                IsTeamLeader = false,
                JoinedAt = SeedData.BaseInstant,
                CurrentLatitude = null,
                CurrentLongitude = null,
                LastUpdated = null
            },
            new TeamMember
            {
                Id = SeedData.SessionTwoMemberId,
                SessionId = SeedData.SessionTwoId,
                TeamId = SeedData.SessionTwoTeamId,
                UserId = SeedData.SpectatorUserId,
                GuestName = null,
                IsTeamLeader = true,
                JoinedAt = SeedData.BaseInstant,
                CurrentLatitude = 48.22,
                CurrentLongitude = 16.32,
                LastUpdated = SeedData.BaseInstant
            }
        );
    }

    private static void InsertGeofencePoints(DatabaseContext ctx)
    {
        ctx.GeofencePoints.AddRange(
            new GeofencePoint
            {
                Id = SeedData.GeofencePointOneId,
                SessionId = SeedData.SessionId,
                Latitude = 48.2,
                Longitude = 16.3,
                SequenceOrder = 1
            },
            new GeofencePoint
            {
                Id = SeedData.GeofencePointTwoId,
                SessionId = SeedData.SessionId,
                Latitude = 48.21,
                Longitude = 16.31,
                SequenceOrder = 2
            }
        );
    }

    private static void InsertLocationLogs(DatabaseContext ctx)
    {
        ctx.LocationLogs.AddRange(
            new LocationLog
            {
                Id = SeedData.LocationLogOneId,
                MemberId = SeedData.DetectiveMemberId,
                Timestamp = SeedData.BaseInstant.Plus(Duration.FromMinutes(10)),
                Latitude = 48.2,
                Longitude = 16.3,
                AccuracyMeters = 5.0,
                TransportMode = TransportMode.Foot,
                IsRevealedPosition = false
            },
            new LocationLog
            {
                Id = SeedData.LocationLogTwoId,
                MemberId = SeedData.DetectiveMemberId,
                Timestamp = SeedData.BaseInstant.Plus(Duration.FromMinutes(20)),
                Latitude = 48.21,
                Longitude = 16.31,
                AccuracyMeters = 5.5,
                TransportMode = TransportMode.Bus,
                IsRevealedPosition = true
            }
        );
    }

    private static void InsertPowerUpUsages(DatabaseContext ctx)
    {
        ctx.PowerUpUsages.AddRange(
            new PowerUpUsage
            {
                Id = SeedData.PowerUpUsageId,
                MemberId = SeedData.DetectiveMemberId,
                PowerUpType = PowerUpType.BlackTicket,
                UsedAt = SeedData.BaseInstant.Plus(Duration.FromMinutes(30))
            }
        );
    }
}
