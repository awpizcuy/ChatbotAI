using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyChatbotBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAiResponseToUserQuestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiResponse",
                table: "UserQuestions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiResponse",
                table: "UserQuestions");
        }
    }
}
