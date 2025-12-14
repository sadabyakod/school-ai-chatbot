-- Create EvaluationSheets table for storing evaluation sheet/answer scheme metadata
-- Run this on Azure SQL Database: smartstudysqlsrv.database.windows.net / smartstudydb

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EvaluationSheets')
BEGIN
    CREATE TABLE [dbo].[EvaluationSheets] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [FileName] NVARCHAR(500) NOT NULL,
        [BlobUrl] NVARCHAR(500) NOT NULL,
        [UploadedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        [UploadedBy] NVARCHAR(200) NULL,
        [Subject] NVARCHAR(100) NOT NULL,
        [Grade] NVARCHAR(50) NOT NULL,
        [State] NVARCHAR(100) NOT NULL DEFAULT 'Karnataka',
        [Medium] NVARCHAR(50) NOT NULL DEFAULT 'English',
        [AcademicYear] NVARCHAR(20) NULL,
        [Chapter] NVARCHAR(200) NULL,
        [SheetType] NVARCHAR(50) NOT NULL DEFAULT 'Model',
        [FileSize] BIGINT NOT NULL DEFAULT 0,
        [ContentType] NVARCHAR(100) NULL
    );

    -- Create indexes for common queries
    CREATE INDEX [IX_EvaluationSheets_Subject] ON [dbo].[EvaluationSheets] ([Subject]);
    CREATE INDEX [IX_EvaluationSheets_Grade] ON [dbo].[EvaluationSheets] ([Grade]);
    CREATE INDEX [IX_EvaluationSheets_State] ON [dbo].[EvaluationSheets] ([State]);
    CREATE INDEX [IX_EvaluationSheets_AcademicYear] ON [dbo].[EvaluationSheets] ([AcademicYear]);
    CREATE INDEX [IX_EvaluationSheets_UploadedAt] ON [dbo].[EvaluationSheets] ([UploadedAt] DESC);

    PRINT 'EvaluationSheets table created successfully.';
END
ELSE
BEGIN
    PRINT 'EvaluationSheets table already exists.';
END
