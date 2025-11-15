# EF Core Migration Commands

## Generate New Migration

Run this command from the `SchoolAiChatbotBackend` directory:

```powershell
# Add migration for Azure Functions tables
dotnet ef migrations add AddAzureFunctionsTables --context AppDbContext

# Review the generated migration file in Migrations/ folder
# It should include CreateTable operations for FileChunks and ChunkEmbeddings
```

## Apply Migration to Database

```powershell
# Update local database
dotnet ef database update

# Update Azure SQL Database (use connection string)
dotnet ef database update --connection "YOUR_AZURE_SQL_CONNECTION_STRING"
```

## Manual SQL Script (Alternative)

If you prefer to run SQL manually, use this script:

```sql
-- =====================================================
-- Azure Functions Integration Tables
-- =====================================================

-- FileChunks table (stores text chunks from uploaded files)
CREATE TABLE [dbo].[FileChunks] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FileId] INT NOT NULL,
    [ChunkText] NVARCHAR(MAX) NOT NULL,
    [ChunkIndex] INT NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
    [Subject] NVARCHAR(100) NULL,
    [Grade] NVARCHAR(50) NULL,
    [Chapter] NVARCHAR(200) NULL,
    CONSTRAINT [FK_FileChunks_UploadedFiles] FOREIGN KEY ([FileId]) 
        REFERENCES [dbo].[UploadedFiles] ([Id]) ON DELETE CASCADE
);

-- ChunkEmbeddings table (stores vector embeddings for similarity search)
CREATE TABLE [dbo].[ChunkEmbeddings] (
    [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ChunkId] INT NOT NULL,
    [EmbeddingVector] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [FK_ChunkEmbeddings_FileChunks] FOREIGN KEY ([ChunkId]) 
        REFERENCES [dbo].[FileChunks] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [UQ_ChunkEmbeddings_ChunkId] UNIQUE ([ChunkId])
);

-- Indexes for performance
CREATE NONCLUSTERED INDEX [IX_FileChunks_FileId_ChunkIndex] 
    ON [dbo].[FileChunks] ([FileId], [ChunkIndex]);

CREATE NONCLUSTERED INDEX [IX_FileChunks_Subject_Grade_Chapter] 
    ON [dbo].[FileChunks] ([Subject], [Grade], [Chapter]);

CREATE NONCLUSTERED INDEX [IX_ChunkEmbeddings_ChunkId] 
    ON [dbo].[ChunkEmbeddings] ([ChunkId]);

-- =====================================================
-- Update UploadedFiles table schema (if needed)
-- =====================================================

-- Check if columns exist, add if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'BlobUrl')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [BlobUrl] NVARCHAR(500) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Subject')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [Subject] NVARCHAR(100) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Grade')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [Grade] NVARCHAR(50) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Chapter')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [Chapter] NVARCHAR(200) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'UploadedBy')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [UploadedBy] NVARCHAR(200) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'TotalChunks')
BEGIN
    ALTER TABLE [dbo].[UploadedFiles] ADD [TotalChunks] INT NULL;
END

-- =====================================================
-- ChatHistory and StudyNotes tables
-- =====================================================

-- ChatHistory (if not already created)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatHistories')
BEGIN
    CREATE TABLE [dbo].[ChatHistories] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [SessionId] NVARCHAR(450) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Reply] NVARCHAR(MAX) NOT NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
        [ContextUsed] NVARCHAR(MAX) NULL,
        [ContextCount] INT NOT NULL DEFAULT 0,
        [AuthenticatedUserId] INT NULL,
        CONSTRAINT [FK_ChatHistories_Users] FOREIGN KEY ([AuthenticatedUserId]) 
            REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [IX_ChatHistories_UserId_SessionId_Timestamp] 
        ON [dbo].[ChatHistories] ([UserId], [SessionId], [Timestamp]);
END

-- StudyNotes (if not already created)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StudyNotes')
BEGIN
    CREATE TABLE [dbo].[StudyNotes] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(450) NOT NULL,
        [Topic] NVARCHAR(500) NOT NULL,
        [GeneratedNotes] NVARCHAR(MAX) NOT NULL,
        [SourceChunks] NVARCHAR(MAX) NULL,
        [Subject] NVARCHAR(100) NULL,
        [Grade] NVARCHAR(50) NULL,
        [Chapter] NVARCHAR(200) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
        [Rating] INT NULL,
        [AuthenticatedUserId] INT NULL,
        CONSTRAINT [FK_StudyNotes_Users] FOREIGN KEY ([AuthenticatedUserId]) 
            REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX [IX_StudyNotes_UserId_CreatedAt] 
        ON [dbo].[StudyNotes] ([UserId], [CreatedAt]);
END

-- =====================================================
-- Verify Migration
-- =====================================================

SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS ColumnCount
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_NAME IN ('FileChunks', 'ChunkEmbeddings', 'UploadedFiles', 'ChatHistories', 'StudyNotes')
ORDER BY TABLE_NAME;

PRINT 'Migration completed successfully!';
```

## Rollback Migration (if needed)

```powershell
# List all migrations
dotnet ef migrations list

# Remove last migration (before applying to database)
dotnet ef migrations remove

# Revert database to previous migration
dotnet ef database update PreviousMigrationName
```

## Test Migration

```sql
-- Test FileChunks table
INSERT INTO FileChunks (FileId, ChunkText, ChunkIndex, Subject, Grade, Chapter)
VALUES (1, 'Test chunk content', 0, 'Mathematics', 'Grade 10', 'Chapter 1');

-- Test ChunkEmbeddings table
INSERT INTO ChunkEmbeddings (ChunkId, EmbeddingVector)
VALUES (1, '[0.1, 0.2, 0.3]');

-- Verify
SELECT fc.*, ce.Id as EmbeddingId 
FROM FileChunks fc
LEFT JOIN ChunkEmbeddings ce ON ce.ChunkId = fc.Id;
```

## Connection Strings

### Local Development

Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "SqlDb": "Server=localhost;Database=SchoolAiChatbot;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### Azure SQL

```json
{
  "ConnectionStrings": {
    "SqlDb": "Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;Persist Security Info=False;User ID=YOUR_USER;Password=YOUR_PASS;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

## Troubleshooting

### Error: "No DbContext was found"

```powershell
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Or update if already installed
dotnet tool update --global dotnet-ef
```

### Error: "Build failed"

```powershell
# Clean and rebuild
dotnet clean
dotnet build
dotnet ef migrations add AddAzureFunctionsTables
```

### Error: "Cannot connect to database"

- Verify connection string in `appsettings.json`
- Check firewall rules in Azure SQL
- Test connection using SQL Server Management Studio or Azure Data Studio
