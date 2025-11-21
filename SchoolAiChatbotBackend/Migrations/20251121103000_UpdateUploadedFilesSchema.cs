using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAiChatbotBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUploadedFilesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old columns
            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "EmbeddingDimension",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "EmbeddingVector",
                table: "UploadedFiles");

            // Update FileName column
            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UploadedFiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // Add new columns
            migrationBuilder.AddColumn<string>(
                name: "BlobUrl",
                table: "UploadedFiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "UploadedFiles",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<string>(
                name: "UploadedBy",
                table: "UploadedFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "UploadedFiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "UploadedFiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Chapter",
                table: "UploadedFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalChunks",
                table: "UploadedFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UploadedFiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove new columns
            migrationBuilder.DropColumn(
                name: "BlobUrl",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Chapter",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "TotalChunks",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UploadedFiles");

            // Restore old columns
            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadDate",
                table: "UploadedFiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "EmbeddingDimension",
                table: "UploadedFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingVector",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);
        }
    }
}
