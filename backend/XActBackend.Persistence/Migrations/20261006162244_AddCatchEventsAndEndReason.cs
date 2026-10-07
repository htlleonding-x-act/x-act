using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatchEventsAndEndReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndReason",
                schema: "XActBackend",
                table: "GameSession",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatchEvent",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CatchingTeamId = table.Column<int>(type: "integer", nullable: false),
                    CaughtTeamId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatchEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatchEvent_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatchEvent_Team_CatchingTeamId",
                        column: x => x.CatchingTeamId,
                        principalSchema: "XActBackend",
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CatchEvent_Team_CaughtTeamId",
                        column: x => x.CaughtTeamId,
                        principalSchema: "XActBackend",
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CatchEvent_CatchingTeamId",
                schema: "XActBackend",
                table: "CatchEvent",
                column: "CatchingTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchEvent_CaughtTeamId",
                schema: "XActBackend",
                table: "CatchEvent",
                column: "CaughtTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CatchEvent_SessionId_OccurredAt",
                schema: "XActBackend",
                table: "CatchEvent",
                columns: new[] { "SessionId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatchEvent",
                schema: "XActBackend");

            migrationBuilder.DropColumn(
                name: "EndReason",
                schema: "XActBackend",
                table: "GameSession");
        }
    }
}
