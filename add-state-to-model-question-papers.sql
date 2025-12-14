-- Migration: Add State column to ModelQuestionPapers table
-- Run this script on Azure SQL Database: smartstudysqlsrv.database.windows.net / smartstudydb

-- Add State column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ModelQuestionPapers') AND name = 'State')
BEGIN
    ALTER TABLE ModelQuestionPapers
    ADD [State] NVARCHAR(100) NOT NULL DEFAULT 'Karnataka';
    
    PRINT 'State column added to ModelQuestionPapers table';
END
ELSE
BEGIN
    PRINT 'State column already exists in ModelQuestionPapers table';
END
GO
