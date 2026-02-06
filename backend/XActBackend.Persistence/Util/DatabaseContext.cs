using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Util;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public const string SchemaName = "XActBackend";

    public DbSet<User> Users { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<GeofencePoint> GeofencePoints { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<LocationLog> LocationLogs { get; set; }
    public DbSet<PowerUpUsage> PowerUpUsages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<User>(ConfigureUser);
        modelBuilder.Entity<GameSession>(ConfigureGameSession);
        modelBuilder.Entity<GeofencePoint>(ConfigureGeofencePoint);
        modelBuilder.Entity<Team>(ConfigureTeam);
        modelBuilder.Entity<TeamMember>(ConfigureTeamMember);
        modelBuilder.Entity<LocationLog>(ConfigureLocationLog);
        modelBuilder.Entity<PowerUpUsage>(ConfigurePowerUpUsage);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Conventions.Remove<TableNameFromDbSetConvention>();

        configurationBuilder.Properties<AccountType>().HaveConversion<string>();
        configurationBuilder.Properties<SessionStatus>().HaveConversion<string>();
        configurationBuilder.Properties<TeamRole>().HaveConversion<string>();
        configurationBuilder.Properties<TransportMode>().HaveConversion<string>();
        configurationBuilder.Properties<PowerUpType>().HaveConversion<string>();
    }

    private static void ConfigureUser(EntityTypeBuilder<User> user)
    {
        user.HasIndex(e => e.Email).IsUnique();
        user.HasIndex(e => e.Username).IsUnique();
    }

    private static void ConfigureGameSession(EntityTypeBuilder<GameSession> gameSession)
    {
        gameSession.Property(e => e.JoinCode).HasMaxLength(6);
        gameSession.HasIndex(e => e.JoinCode).IsUnique();

        gameSession
            .HasOne(e => e.Host)
            .WithMany(u => u.HostedSessions)
            .HasForeignKey(e => e.HostUserId);
    }

    private static void ConfigureGeofencePoint(EntityTypeBuilder<GeofencePoint> geofencePoint)
    {
        geofencePoint
            .HasOne(e => e.Session)
            .WithMany(s => s.GeofencePoints)
            .HasForeignKey(e => e.SessionId);
    }

    private static void ConfigureTeam(EntityTypeBuilder<Team> team)
    {
        team.Property(e => e.ColorCode).HasMaxLength(7);

        team
            .HasOne(e => e.Session)
            .WithMany(s => s.Teams)
            .HasForeignKey(e => e.SessionId);
    }

    private static void ConfigureTeamMember(EntityTypeBuilder<TeamMember> teamMember)
    {
        teamMember
            .HasOne(e => e.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(e => e.TeamId);

        teamMember
            .HasOne(e => e.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(e => e.UserId);
    }

    private static void ConfigureLocationLog(EntityTypeBuilder<LocationLog> locationLog)
    {
        locationLog
            .HasOne(e => e.Member)
            .WithMany(m => m.LocationLogs)
            .HasForeignKey(e => e.MemberId);
    }

    private static void ConfigurePowerUpUsage(EntityTypeBuilder<PowerUpUsage> powerUpUsage)
    {
        powerUpUsage
            .HasOne(e => e.Member)
            .WithMany(m => m.PowerUpUsages)
            .HasForeignKey(e => e.MemberId);
    }
}
