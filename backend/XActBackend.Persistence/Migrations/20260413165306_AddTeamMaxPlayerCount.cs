using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMaxPlayerCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxPlayerCount",
                schema: "XActBackend",
                table: "Team",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Team_MaxPlayerCount_Positive",
                schema: "XActBackend",
                table: "Team",
                sql: "\"MaxPlayerCount\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Team_MaxPlayerCount_Positive",
                schema: "XActBackend",
                table: "Team");

            migrationBuilder.DropColumn(
                name: "MaxPlayerCount",
                schema: "XActBackend",
                table: "Team");
        }
    }
}
