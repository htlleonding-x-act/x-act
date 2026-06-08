using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportKickVotingAndOffenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KickVote",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    TargetMemberId = table.Column<int>(type: "integer", nullable: true),
                    InitiatorMemberId = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KickVote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KickVote_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KickVote_TeamMember_InitiatorMemberId",
                        column: x => x.InitiatorMemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_KickVote_TeamMember_TargetMemberId",
                        column: x => x.TargetMemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Offense",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SessionId = table.Column<int>(type: "integer", nullable: false),
                    MemberId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ClearedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offense", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offense_GameSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "XActBackend",
                        principalTable: "GameSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offense_TeamMember_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KickVoteBallot",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KickVoteId = table.Column<int>(type: "integer", nullable: false),
                    VoterMemberId = table.Column<int>(type: "integer", nullable: true),
                    Approve = table.Column<bool>(type: "boolean", nullable: false),
                    CastAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KickVoteBallot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KickVoteBallot_KickVote_KickVoteId",
                        column: x => x.KickVoteId,
                        principalSchema: "XActBackend",
                        principalTable: "KickVote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KickVoteBallot_TeamMember_VoterMemberId",
                        column: x => x.VoterMemberId,
                        principalSchema: "XActBackend",
                        principalTable: "TeamMember",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KickVote_InitiatorMemberId",
                schema: "XActBackend",
                table: "KickVote",
                column: "InitiatorMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_KickVote_SessionId_Status",
                schema: "XActBackend",
                table: "KickVote",
                columns: new[] { "SessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_KickVote_TargetMemberId",
                schema: "XActBackend",
                table: "KickVote",
                column: "TargetMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_KickVoteBallot_KickVoteId_VoterMemberId",
                schema: "XActBackend",
                table: "KickVoteBallot",
                columns: new[] { "KickVoteId", "VoterMemberId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KickVoteBallot_VoterMemberId",
                schema: "XActBackend",
                table: "KickVoteBallot",
                column: "VoterMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Offense_MemberId_Type_Status",
                schema: "XActBackend",
                table: "Offense",
                columns: new[] { "MemberId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Offense_SessionId_Status",
                schema: "XActBackend",
                table: "Offense",
                columns: new[] { "SessionId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KickVoteBallot",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "Offense",
                schema: "XActBackend");

            migrationBuilder.DropTable(
                name: "KickVote",
                schema: "XActBackend");
        }
    }
}
