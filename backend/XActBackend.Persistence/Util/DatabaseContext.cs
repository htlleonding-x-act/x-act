using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using XActBackend.Persistence.Model;

namespace XActBackend.Persistence.Util;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public const string SchemaName = "XActBackend";

    public DbSet<User> Users { get; set; }
    public DbSet<UserAuthIdentity> UserAuthIdentities { get; set; }
    public DbSet<GameSession> GameSessions { get; set; }
    public DbSet<GeofencePoint> GeofencePoints { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<LocationLog> LocationLogs { get; set; }
    public DbSet<PowerUpUsage> PowerUpUsages { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<KickVote> KickVotes { get; set; }
    public DbSet<KickVoteBallot> KickVoteBallots { get; set; }
    public DbSet<Offense> Offenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<User>(ConfigureUser);
        modelBuilder.Entity<UserAuthIdentity>(ConfigureUserAuthIdentity);
        modelBuilder.Entity<GameSession>(ConfigureGameSession);
        modelBuilder.Entity<GeofencePoint>(ConfigureGeofencePoint);
        modelBuilder.Entity<Team>(ConfigureTeam);
        modelBuilder.Entity<TeamMember>(ConfigureTeamMember);
        modelBuilder.Entity<LocationLog>(ConfigureLocationLog);
        modelBuilder.Entity<PowerUpUsage>(ConfigurePowerUpUsage);
        modelBuilder.Entity<ChatMessage>(ConfigureChatMessage);
        modelBuilder.Entity<KickVote>(ConfigureKickVote);
        modelBuilder.Entity<KickVoteBallot>(ConfigureKickVoteBallot);
        modelBuilder.Entity<Offense>(ConfigureOffense);
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
        configurationBuilder.Properties<KickVoteStatus>().HaveConversion<string>();
        configurationBuilder.Properties<OffenseType>().HaveConversion<string>();
        configurationBuilder.Properties<OffenseStatus>().HaveConversion<string>();
    }

    private static void ConfigureUser(EntityTypeBuilder<User> user)
    {
        user.Property(e => e.Username).HasMaxLength(50);
        user.Property(e => e.Email).HasMaxLength(100);

        user.HasIndex(e => e.Email).IsUnique();
        user.HasIndex(e => e.Username).IsUnique();
    }

    private static void ConfigureUserAuthIdentity(EntityTypeBuilder<UserAuthIdentity> authIdentity)
    {
        authIdentity.Property(e => e.ProviderSubject).HasMaxLength(255);

        authIdentity
            .HasOne(e => e.User)
            .WithMany(u => u.AuthIdentities)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        authIdentity.HasIndex(e => e.ProviderSubject).IsUnique();
        authIdentity.HasIndex(e => e.UserId).IsUnique();
    }

    private static void ConfigureGameSession(EntityTypeBuilder<GameSession> gameSession)
    {
        gameSession.Property(e => e.SessionName).HasMaxLength(120);
        gameSession.Property(e => e.JoinCode).HasMaxLength(6);
        gameSession.HasIndex(e => e.JoinCode).IsUnique();

        gameSession
            .HasOne(e => e.Host)
            .WithMany(u => u.HostedSessions)
            .HasForeignKey(e => e.HostUserId)
            .OnDelete(DeleteBehavior.Restrict);

        gameSession.HasIndex(e => e.HostUserId);
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
        team.Property(e => e.MaxPlayerCount).HasDefaultValue(Team.DefaultMaxPlayerCount);

        team.ToTable(t => t.HasCheckConstraint(
            "CK_Team_MaxPlayerCount_Positive",
            "\"MaxPlayerCount\" > 0"
        ));

        team
            .HasOne(e => e.Session)
            .WithMany(s => s.Teams)
            .HasForeignKey(e => e.SessionId);
    }

    private static void ConfigureTeamMember(EntityTypeBuilder<TeamMember> teamMember)
    {
        teamMember.Property(e => e.GuestName).HasMaxLength(50);

        teamMember
            .HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        teamMember
            .HasOne(e => e.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(e => e.TeamId);

        teamMember
            .HasOne(e => e.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        teamMember.ToTable(t => t.HasCheckConstraint(
            "CK_TeamMember_UserOrGuest",
            "(\"UserId\" IS NOT NULL AND \"GuestName\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestName\" IS NOT NULL)"
        ));

        teamMember.HasIndex(e => new { e.SessionId, e.UserId }).IsUnique();
        teamMember.HasIndex(e => new { e.TeamId, e.GuestName }).IsUnique();
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

    private static void ConfigureChatMessage(EntityTypeBuilder<ChatMessage> chatMessage)
    {
        chatMessage.Property(e => e.Content).HasMaxLength(ChatMessage.MaxContentLength);
        chatMessage.Property(e => e.SenderName).HasMaxLength(ChatMessage.MaxSenderNameLength);

        chatMessage
            .HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        chatMessage
            .HasOne(e => e.Team)
            .WithMany()
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        chatMessage
            .HasOne(e => e.Sender)
            .WithMany()
            .HasForeignKey(e => e.SenderMemberId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        chatMessage
            .HasIndex(e => new { e.SessionId, e.TeamId, e.SentAt, e.Id })
            .IsDescending(false, false, true, true);
    }

    private static void ConfigureKickVote(EntityTypeBuilder<KickVote> kickVote)
    {
        kickVote.Property(e => e.Reason).HasMaxLength(KickVote.MaxReasonLength);

        kickVote
            .HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Target and initiator references survive the member leaving: a resolved vote stays as
        // history even after its target was kicked, so the member link is cleared, not cascaded.
        kickVote
            .HasOne(e => e.TargetMember)
            .WithMany()
            .HasForeignKey(e => e.TargetMemberId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        kickVote
            .HasOne(e => e.InitiatorMember)
            .WithMany()
            .HasForeignKey(e => e.InitiatorMemberId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        kickVote.HasIndex(e => new { e.SessionId, e.Status });
    }

    private static void ConfigureKickVoteBallot(EntityTypeBuilder<KickVoteBallot> ballot)
    {
        ballot
            .HasOne(e => e.KickVote)
            .WithMany(v => v.Ballots)
            .HasForeignKey(e => e.KickVoteId)
            .OnDelete(DeleteBehavior.Cascade);

        ballot
            .HasOne(e => e.Voter)
            .WithMany()
            .HasForeignKey(e => e.VoterMemberId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // A member may cast at most one ballot per vote. Postgres treats nulls as distinct, so
        // cleared voter references (after a member leaves) do not collide in this unique index.
        ballot.HasIndex(e => new { e.KickVoteId, e.VoterMemberId }).IsUnique();
    }

    private static void ConfigureOffense(EntityTypeBuilder<Offense> offense)
    {
        offense
            .HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // An offense belongs to a present member; when the member is removed the offense goes with them.
        offense
            .HasOne(e => e.Member)
            .WithMany()
            .HasForeignKey(e => e.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        offense.HasIndex(e => new { e.SessionId, e.Status });
        offense.HasIndex(e => new { e.MemberId, e.Type, e.Status });
    }
}
