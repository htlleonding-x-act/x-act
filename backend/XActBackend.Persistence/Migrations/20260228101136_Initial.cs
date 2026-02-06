using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    SubscriptionEndDate = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    TotalWins = table.Column<int>(type: "integer", nullable: false),
                    TotalGamesPlayed = table.Column<int>(type: "integer", nullable: false)
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
                    HostUserId = table.Column<int>(type: "integer", nullable: false),
                    JoinCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    PlannedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MrXRevealInterval = table.Column<int>(type: "integer", nullable: false)
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
                    IsCaught = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
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
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IsTeamLeader = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentLatitude = table.Column<double>(type: "double precision", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LastUpdated = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMember", x => x.Id);
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
                name: "IX_TeamMember_TeamId",
                schema: "XActBackend",
                table: "TeamMember",
                column: "TeamId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
