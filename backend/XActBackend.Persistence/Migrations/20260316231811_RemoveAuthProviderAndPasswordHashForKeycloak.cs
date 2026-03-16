using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuthProviderAndPasswordHashForKeycloak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAuthIdentity_Provider_ProviderSubject",
                schema: "XActBackend",
                table: "UserAuthIdentity");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthIdentity_UserId_Provider",
                schema: "XActBackend",
                table: "UserAuthIdentity");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "XActBackend",
                table: "UserAuthIdentity");

            migrationBuilder.DropColumn(
                name: "Provider",
                schema: "XActBackend",
                table: "UserAuthIdentity");

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
            migrationBuilder.DropIndex(
                name: "IX_UserAuthIdentity_ProviderSubject",
                schema: "XActBackend",
                table: "UserAuthIdentity");

            migrationBuilder.DropIndex(
                name: "IX_UserAuthIdentity_UserId",
                schema: "XActBackend",
                table: "UserAuthIdentity");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "XActBackend",
                table: "UserAuthIdentity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                schema: "XActBackend",
                table: "UserAuthIdentity",
                type: "text",
                nullable: false,
                defaultValue: "");

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
        }
    }
}
