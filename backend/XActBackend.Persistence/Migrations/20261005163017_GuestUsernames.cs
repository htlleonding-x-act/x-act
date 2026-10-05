using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GuestUsernames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Username",
                schema: "XActBackend",
                table: "User");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                schema: "XActBackend",
                table: "User",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGuest",
                schema: "XActBackend",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                schema: "XActBackend",
                table: "User",
                column: "Username",
                unique: true,
                filter: "\"IsGuest\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Username",
                schema: "XActBackend",
                table: "User");

            migrationBuilder.DropColumn(
                name: "IsGuest",
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
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Username",
                schema: "XActBackend",
                table: "User",
                column: "Username",
                unique: true);
        }
    }
}
