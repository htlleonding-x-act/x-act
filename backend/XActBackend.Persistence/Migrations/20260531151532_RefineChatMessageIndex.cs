using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XActBackend.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefineChatMessageIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessage_SessionId_TeamId_SentAt",
                schema: "XActBackend",
                table: "ChatMessage");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SessionId_TeamId_SentAt_Id",
                schema: "XActBackend",
                table: "ChatMessage",
                columns: new[] { "SessionId", "TeamId", "SentAt", "Id" },
                descending: new[] { false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChatMessage_SessionId_TeamId_SentAt_Id",
                schema: "XActBackend",
                table: "ChatMessage");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SessionId_TeamId_SentAt",
                schema: "XActBackend",
                table: "ChatMessage",
                columns: new[] { "SessionId", "TeamId", "SentAt" });
        }
    }
}
