using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserIdMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "XActBackend");

            migrationBuilder.CreateTable(
                name: "User",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    SubscriptionEndDate = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    TotalWins = table.Column<int>(type: "integer", nullable: false),
                    TotalGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameSession",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HostUserId = table.Column<string>(type: "text", nullable: false),
                    SessionName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    JoinCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    PlannedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MrXRevealInterval = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameSession_User_HostUserId",
                        column: x => x.HostUserId,
                        principalSchema: "XActBackend",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthIdentity",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    ProviderSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthIdentity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAuthIdentity_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "XActBackend",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeofencePoint",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeofencePoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeofencePoint_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Team",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    ColorCode = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    MaxPlayerCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    IsCaught = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                    table.CheckConstraint("CK_Team_MaxPlayerCount_Positive", "\"MaxPlayerCount\" > 0");
                    table.ForeignKey(
                        name: "FK_Team_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMember",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    GuestName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsTeamLeader = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LastUpdated = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMember", x => x.Id);
                    table.CheckConstraint("CK_TeamMember_UserOrGuest", "(\"UserId\" IS NOT NULL AND \"GuestName\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestName\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TeamMember_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMember_Team_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "XActBackend",
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMember_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "XActBackend",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessage",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    SenderMemberId = table.Column<int>(type: "integer", nullable: true),
                    SenderTeamId = table.Column<int>(type: "integer", nullable: true),
                    SenderName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SentAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessage_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessage_TeamMember_SenderMemberId",
                        column: x => x.SenderMemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ChatMessage_Team_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "XActBackend",
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationLog",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    AccuracyMeters = table.Column<double>(type: "double precision", nullable: false),
                    TransportMode = table.Column<string>(type: "text", nullable: false),
                    IsRevealedPosition = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationLog_TeamMember_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PowerUpUsage",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    PowerUpType = table.Column<string>(type: "text", nullable: false),
                    UsedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerUpUsage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PowerUpUsage_TeamMember_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SenderMemberId",
                schema: "XActBackend",
                table: "ChatMessage",
                column: "SenderMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SessionId_TeamId_SentAt_Id",
                schema: "XActBackend",
                table: "ChatMessage",
                columns: new[] { "SessionId", "TeamId", "SentAt", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_TeamId",
                schema: "XActBackend",
                table: "ChatMessage",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSession_HostUserId",
                schema: "XActBackend",
                table: "GameSession",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSession_JoinCode",
                schema: "XActBackend",
                table: "GameSession",
                column: "JoinCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeofencePoint_SessionId",
                schema: "XActBackend",
                table: "GeofencePoint",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationLog_MemberId",
                schema: "XActBackend",
                table: "LocationLog",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PowerUpUsage_MemberId",
                schema: "XActBackend",
                table: "PowerUpUsage",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Team_SessionId",
                schema: "XActBackend",
                table: "Team",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_SessionId_UserId",
                schema: "XActBackend",
                table: "TeamMember",
                columns: new[] { "SessionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId_GuestName",
                schema: "XActBackend",
                table: "TeamMember",
                columns: new[] { "TeamId", "GuestName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_UserId",
                schema: "XActBackend",
                table: "TeamMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                schema: "XActBackend",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                schema: "XActBackend",
                table: "User",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthIdentity_ProviderSubject",
                schema: "XActBackend",
                table: "UserAuthIdentity",
                column: "ProviderSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthIdentity_UserId",
                schema: "XActBackend",
                table: "UserAuthIdentity",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessage",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "GeofencePoint",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "LocationLog",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "PowerUpUsage",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "UserAuthIdentity",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "TeamMember",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "Team",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "GameSession",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "User",
                schema: "XActBackend");
        }
    }
}
