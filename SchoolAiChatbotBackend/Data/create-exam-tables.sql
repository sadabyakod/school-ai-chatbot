-- SQL Script to create GeneratedExams table in Azure SQL
-- Run this script manually in Azure SQL Database or via SSMS/Azure Portal Query Editor

-- Create the GeneratedExams table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='GeneratedExams' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[GeneratedExams] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ExamId] NVARCHAR(200) NOT NULL,
        [Subject] NVARCHAR(100) NOT NULL,
        [Grade] NVARCHAR(50) NULL,
        [Chapter] NVARCHAR(200) NULL,
        [Difficulty] NVARCHAR(50) NULL,
        [TotalMarks] INT NOT NULL DEFAULT 0,
        [DurationMinutes] INT NOT NULL DEFAULT 0,
        [ExamContentJson] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_GeneratedExams] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create unique index on ExamId for fast lookup
    CREATE UNIQUE NONCLUSTERED INDEX [IX_GeneratedExams_ExamId] 
    ON [dbo].[GeneratedExams] ([ExamId]);

    -- Create composite index for filtering by subject, grade, chapter
    CREATE NONCLUSTERED INDEX [IX_GeneratedExams_Subject_Grade_Chapter] 
    ON [dbo].[GeneratedExams] ([Subject], [Grade], [Chapter]);

    -- Create index on CreatedAt for recent exams queries
    CREATE NONCLUSTERED INDEX [IX_GeneratedExams_CreatedAt] 
    ON [dbo].[GeneratedExams] ([CreatedAt] DESC);

    PRINT 'GeneratedExams table created successfully';
END
ELSE
BEGIN
    PRINT 'GeneratedExams table already exists';
END
GO

-- Create the SubjectiveRubrics table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SubjectiveRubrics' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[SubjectiveRubrics] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [ExamId] NVARCHAR(100) NOT NULL,
        [QuestionId] NVARCHAR(50) NOT NULL,
        [TotalMarks] INT NOT NULL DEFAULT 0,
        [StepsJson] NVARCHAR(MAX) NOT NULL,
        [QuestionText] NVARCHAR(MAX) NULL,
        [ModelAnswer] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_SubjectiveRubrics] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Create unique composite index on ExamId + QuestionId
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SubjectiveRubrics_ExamId_QuestionId] 
    ON [dbo].[SubjectiveRubrics] ([ExamId], [QuestionId]);

    -- Create index on ExamId for getting all rubrics for an exam
    CREATE NONCLUSTERED INDEX [IX_SubjectiveRubrics_ExamId] 
    ON [dbo].[SubjectiveRubrics] ([ExamId]);

    PRINT 'SubjectiveRubrics table created successfully';
END
ELSE
BEGIN
    PRINT 'SubjectiveRubrics table already exists';
END
GO
