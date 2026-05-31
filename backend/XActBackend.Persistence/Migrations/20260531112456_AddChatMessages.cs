using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SenderMemberId",
                schema: "XActBackend",
                table: "ChatMessage",
                column: "SenderMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SessionId_TeamId_SentAt",
                schema: "XActBackend",
                table: "ChatMessage",
                columns: new[] { "SessionId", "TeamId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_TeamId",
                schema: "XActBackend",
                table: "ChatMessage",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessage",
                schema: "XActBackend");
        }
    }
}
