using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAiChatbotBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddChatSessionFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "StudyNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShareToken",
                table: "StudyNotes",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "StudyNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tag",
                table: "ChatHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_ShareToken",
                table: "StudyNotes",
                column: "ShareToken",
                unique: true,
                filter: "[ShareToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_Tag",
                table: "ChatHistories",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudyNotes_ShareToken",
                table: "StudyNotes");

            migrationBuilder.DropIndex(
                name: "IX_ChatHistories_Tag",
                table: "ChatHistories");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "StudyNotes");

            migrationBuilder.DropColumn(
                name: "ShareToken",
                table: "StudyNotes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "StudyNotes");

            migrationBuilder.DropColumn(
                name: "Tag",
                table: "ChatHistories");
        }
    }
}
