using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolAiChatbotBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSyllabusChunksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Chapter if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'Chapter' AND Object_ID = Object_ID(N'SyllabusChunks'))
BEGIN
    ALTER TABLE [SyllabusChunks] ADD [Chapter] nvarchar(max) NOT NULL DEFAULT N'';
END
");

            // Add PineconeVectorId if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'PineconeVectorId' AND Object_ID = Object_ID(N'SyllabusChunks'))
BEGIN
    ALTER TABLE [SyllabusChunks] ADD [PineconeVectorId] nvarchar(max) NOT NULL DEFAULT N'';
END
");

            // Add UploadedFileId if missing
            migrationBuilder.Sql(@"
IF NOT EXISTS(SELECT * FROM sys.columns WHERE Name = N'UploadedFileId' AND Object_ID = Object_ID(N'SyllabusChunks'))
BEGIN
    ALTER TABLE [SyllabusChunks] ADD [UploadedFileId] int NOT NULL DEFAULT 0;
END
");

            // Create index and FK only if UploadedFileId column exists and index doesn't already exist
            migrationBuilder.Sql(@"
IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'UploadedFileId' AND Object_ID = Object_ID(N'SyllabusChunks'))
BEGIN
    IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = N'IX_SyllabusChunks_UploadedFileId' AND object_id = Object_ID(N'SyllabusChunks'))
    BEGIN
        CREATE INDEX [IX_SyllabusChunks_UploadedFileId] ON [SyllabusChunks] ([UploadedFileId]);
    END
    IF NOT EXISTS(SELECT * FROM sys.foreign_keys WHERE name = N'FK_SyllabusChunks_UploadedFiles_UploadedFileId')
    BEGIN
        ALTER TABLE [SyllabusChunks] ADD CONSTRAINT [FK_SyllabusChunks_UploadedFiles_UploadedFileId] FOREIGN KEY ([UploadedFileId]) REFERENCES [UploadedFiles]([Id]) ON DELETE CASCADE;
    END
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyllabusChunks_UploadedFiles_UploadedFileId",
                table: "SyllabusChunks");

            migrationBuilder.DropIndex(
                name: "IX_SyllabusChunks_UploadedFileId",
                table: "SyllabusChunks");

            migrationBuilder.DropColumn(
                name: "Chapter",
                table: "SyllabusChunks");

            migrationBuilder.DropColumn(
                name: "PineconeVectorId",
                table: "SyllabusChunks");

            migrationBuilder.DropColumn(
                name: "UploadedFileId",
                table: "SyllabusChunks");
        }
    }
}
