using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAiChatbotBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemovePineconeVectorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PineconeVectorId",
                table: "SyllabusChunks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PineconeVectorId",
                table: "SyllabusChunks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
