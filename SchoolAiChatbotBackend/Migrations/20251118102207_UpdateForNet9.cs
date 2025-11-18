using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAiChatbotBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateForNet9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingDimension",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "EmbeddingVector",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "UploadedFiles");

            migrationBuilder.RenameColumn(
                name: "UploadDate",
                table: "UploadedFiles",
                newName: "UploadedAt");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UploadedFiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BlobUrl",
                table: "UploadedFiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Chapter",
                table: "UploadedFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Grade",
                table: "UploadedFiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UploadedFiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "UploadedFiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalChunks",
                table: "UploadedFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedBy",
                table: "UploadedFiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reply = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContextUsed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextCount = table.Column<int>(type: "int", nullable: false),
                    AuthenticatedUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatHistories_Users_AuthenticatedUserId",
                        column: x => x.AuthenticatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    ChunkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Chapter = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileChunks_UploadedFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceChunks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Chapter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthenticatedUserId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyNotes_Users_AuthenticatedUserId",
                        column: x => x.AuthenticatedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChunkEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChunkId = table.Column<int>(type: "int", nullable: false),
                    EmbeddingVector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChunkEmbeddings_FileChunks_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "FileChunks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_AuthenticatedUserId",
                table: "ChatHistories",
                column: "AuthenticatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatHistories_UserId_SessionId_Timestamp",
                table: "ChatHistories",
                columns: new[] { "UserId", "SessionId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ChunkEmbeddings_ChunkId",
                table: "ChunkEmbeddings",
                column: "ChunkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileChunks_FileId_ChunkIndex",
                table: "FileChunks",
                columns: new[] { "FileId", "ChunkIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_FileChunks_Subject_Grade_Chapter",
                table: "FileChunks",
                columns: new[] { "Subject", "Grade", "Chapter" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_AuthenticatedUserId",
                table: "StudyNotes",
                column: "AuthenticatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_UserId_CreatedAt",
                table: "StudyNotes",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatHistories");

            migrationBuilder.DropTable(
                name: "ChunkEmbeddings");

            migrationBuilder.DropTable(
                name: "StudyNotes");

            migrationBuilder.DropTable(
                name: "FileChunks");

            migrationBuilder.DropColumn(
                name: "BlobUrl",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Chapter",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Grade",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "TotalChunks",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "UploadedFiles");

            migrationBuilder.RenameColumn(
                name: "UploadedAt",
                table: "UploadedFiles",
                newName: "UploadDate");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

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

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "UploadedFiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
