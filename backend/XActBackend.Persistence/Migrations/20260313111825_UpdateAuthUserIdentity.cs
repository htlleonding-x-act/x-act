using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuthUserIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSession_User_HostUserId",
                schema: "XActBackend",
                table: "GameSession");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMember_User_UserId",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_TeamMember_TeamId",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "XActBackend",
                table: "User");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "XActBackend",
                table: "User",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "XActBackend",
                table: "User",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Instant>(
                name: "CreatedAt",
                schema: "XActBackend",
                table: "User",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<Instant>(
                name: "DeletedAt",
                schema: "XActBackend",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "XActBackend",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "XActBackend",
                table: "TeamMember",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                schema: "XActBackend",
                table: "TeamMember",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "JoinedAt",
                schema: "XActBackend",
                table: "TeamMember",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                schema: "XActBackend",
                table: "TeamMember",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Instant>(
                name: "CreatedAt",
                schema: "XActBackend",
                table: "GameSession",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: NodaTime.Instant.FromUnixTimeTicks(0L));

            migrationBuilder.AddColumn<string>(
                name: "SessionName",
                schema: "XActBackend",
                table: "GameSession",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserAuthIdentity",
                schema: "XActBackend",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ProviderSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
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

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeamMember_UserOrGuest",
                schema: "XActBackend",
                table: "TeamMember",
                sql: "(\"UserId\" IS NOT NULL AND \"GuestName\" IS NULL) OR (\"UserId\" IS NULL AND \"GuestName\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthIdentity_Provider_ProviderSubject",
                schema: "XActBackend",
                table: "UserAuthIdentity",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAuthIdentity_UserId_Provider",
                schema: "XActBackend",
                table: "UserAuthIdentity",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GameSession_User_HostUserId",
                schema: "XActBackend",
                table: "GameSession",
                column: "HostUserId",
                principalSchema: "XActBackend",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMember_GameSession_SessionId",
                schema: "XActBackend",
                table: "TeamMember",
                column: "SessionId",
                principalSchema: "XActBackend",
                principalTable: "GameSession",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMember_User_UserId",
                schema: "XActBackend",
                table: "TeamMember",
                column: "UserId",
                principalSchema: "XActBackend",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameSession_User_HostUserId",
                schema: "XActBackend",
                table: "GameSession");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMember_GameSession_SessionId",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamMember_User_UserId",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropTable(
                name: "UserAuthIdentity",
                schema: "XActBackend");

            migrationBuilder.DropIndex(
                name: "IX_TeamMember_SessionId_UserId",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropIndex(
                name: "IX_TeamMember_TeamId_GuestName",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TeamMember_UserOrGuest",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "XActBackend",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "XActBackend",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "XActBackend",
                table: "User");

            migrationBuilder.DropColumn(
                name: "GuestName",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "SessionId",
                schema: "XActBackend",
                table: "TeamMember");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "XActBackend",
                table: "GameSession");

            migrationBuilder.DropColumn(
                name: "SessionName",
                schema: "XActBackend",
                table: "GameSession");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "XActBackend",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "XActBackend",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "XActBackend",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "XActBackend",
                table: "TeamMember",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId",
                schema: "XActBackend",
                table: "TeamMember",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_GameSession_User_HostUserId",
                schema: "XActBackend",
                table: "GameSession",
                column: "HostUserId",
                principalSchema: "XActBackend",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMember_User_UserId",
                schema: "XActBackend",
                table: "TeamMember",
                column: "UserId",
                principalSchema: "XActBackend",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
