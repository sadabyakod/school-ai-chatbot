-- Migration Script: Add FileType, Chapter, and AcademicYear columns to UploadedFiles
-- Run this on Azure SQL Database to support Model Question Papers feature
-- Date: December 14, 2025

-- Add FileType column (Syllabus, ModelQuestionPaper, Notes, etc.)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'FileType')
BEGIN
    ALTER TABLE UploadedFiles ADD FileType NVARCHAR(50) NOT NULL DEFAULT 'Syllabus';
    PRINT 'Added FileType column';
END
ELSE
    PRINT 'FileType column already exists';

-- Add Chapter column (for chapter-wise organization)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Chapter')
BEGIN
    ALTER TABLE UploadedFiles ADD Chapter NVARCHAR(200) NULL;
    PRINT 'Added Chapter column';
END
ELSE
    PRINT 'Chapter column already exists';

-- Add AcademicYear column (e.g., "2024-25")
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'AcademicYear')
BEGIN
    ALTER TABLE UploadedFiles ADD AcademicYear NVARCHAR(20) NULL;
    PRINT 'Added AcademicYear column';
END
ELSE
    PRINT 'AcademicYear column already exists';

-- Create index for faster FileType queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UploadedFiles_FileType')
BEGIN
    CREATE NONCLUSTERED INDEX IX_UploadedFiles_FileType 
    ON UploadedFiles(FileType) 
    INCLUDE (Subject, Grade, Medium, AcademicYear);
    PRINT 'Created index IX_UploadedFiles_FileType';
END

-- Create index for faster Grade + Subject queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UploadedFiles_Grade_Subject')
BEGIN
    CREATE NONCLUSTERED INDEX IX_UploadedFiles_Grade_Subject 
    ON UploadedFiles(Grade, Subject) 
    INCLUDE (FileType, Medium, AcademicYear, BlobUrl);
    PRINT 'Created index IX_UploadedFiles_Grade_Subject';
END

PRINT 'Migration completed successfully!';

-- =========================================
-- SAMPLE DATA: Insert some model question papers
-- =========================================
-- Uncomment below to add sample data

/*
INSERT INTO UploadedFiles (FileName, BlobUrl, UploadedAt, Subject, Grade, Medium, FileType, AcademicYear, Status, TotalChunks)
VALUES 
('Mathematics_2ndPUC_ModelPaper1_2024-25.pdf', 'https://yourblob.blob.core.windows.net/papers/math1.pdf', GETUTCDATE(), 'Mathematics', '2nd PUC', 'English', 'ModelQuestionPaper', '2024-25', 'Completed', 0),
('Physics_2ndPUC_ModelPaper1_2024-25.pdf', 'https://yourblob.blob.core.windows.net/papers/physics1.pdf', GETUTCDATE(), 'Physics', '2nd PUC', 'English', 'ModelQuestionPaper', '2024-25', 'Completed', 0),
('Chemistry_2ndPUC_ModelPaper1_2024-25.pdf', 'https://yourblob.blob.core.windows.net/papers/chemistry1.pdf', GETUTCDATE(), 'Chemistry', '2nd PUC', 'English', 'ModelQuestionPaper', '2024-25', 'Completed', 0),
('Biology_2ndPUC_ModelPaper1_2024-25.pdf', 'https://yourblob.blob.core.windows.net/papers/biology1.pdf', GETUTCDATE(), 'Biology', '2nd PUC', 'English', 'ModelQuestionPaper', '2024-25', 'Completed', 0),
('English_2ndPUC_ModelPaper1_2024-25.pdf', 'https://yourblob.blob.core.windows.net/papers/english1.pdf', GETUTCDATE(), 'English', '2nd PUC', 'English', 'ModelQuestionPaper', '2024-25', 'Completed', 0),
('ComputerScience_2ndPUC_ModelPaper1_2024-25.pdf', 'https://yourblob.blob.core.windows.net/papers/cs1.pdf', GETUTCDATE(), 'Computer Science', '2nd PUC', 'English', 'ModelQuestionPaper', '2024-25', 'Completed', 0);

PRINT 'Sample question papers inserted!';
*/
