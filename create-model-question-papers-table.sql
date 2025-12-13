-- Create ModelQuestionPapers table for storing question papers by class and subject
-- Run this script on Azure SQL Database

-- Create the table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ModelQuestionPapers')
BEGIN
    CREATE TABLE ModelQuestionPapers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(500) NOT NULL,
        BlobUrl NVARCHAR(500) NOT NULL,
        UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UploadedBy NVARCHAR(200) NULL,
        Subject NVARCHAR(100) NOT NULL,
        Grade NVARCHAR(50) NOT NULL,
        Medium NVARCHAR(50) NOT NULL DEFAULT 'English',
        AcademicYear NVARCHAR(20) NULL,
        Chapter NVARCHAR(200) NULL,
        PaperType NVARCHAR(50) NOT NULL DEFAULT 'Model',
        FileSize BIGINT NOT NULL DEFAULT 0,
        ContentType NVARCHAR(100) NULL
    );

    PRINT 'Table ModelQuestionPapers created successfully.';
END
ELSE
BEGIN
    PRINT 'Table ModelQuestionPapers already exists.';
END
GO

-- Create indexes for efficient querying
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ModelQuestionPapers_Grade_Subject')
BEGIN
    CREATE INDEX IX_ModelQuestionPapers_Grade_Subject 
    ON ModelQuestionPapers (Grade, Subject);
    PRINT 'Index IX_ModelQuestionPapers_Grade_Subject created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ModelQuestionPapers_Subject')
BEGIN
    CREATE INDEX IX_ModelQuestionPapers_Subject 
    ON ModelQuestionPapers (Subject);
    PRINT 'Index IX_ModelQuestionPapers_Subject created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ModelQuestionPapers_AcademicYear')
BEGIN
    CREATE INDEX IX_ModelQuestionPapers_AcademicYear 
    ON ModelQuestionPapers (AcademicYear);
    PRINT 'Index IX_ModelQuestionPapers_AcademicYear created.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ModelQuestionPapers_UploadedAt')
BEGIN
    CREATE INDEX IX_ModelQuestionPapers_UploadedAt 
    ON ModelQuestionPapers (UploadedAt DESC);
    PRINT 'Index IX_ModelQuestionPapers_UploadedAt created.';
END
GO

-- Verify table structure
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('ModelQuestionPapers')
ORDER BY c.column_id;
GO
