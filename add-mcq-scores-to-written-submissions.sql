-- Add MCQ score fields to WrittenSubmissions table
-- This allows storing MCQ results alongside subjective answer sheets

USE smartstudydb;
GO

-- Add McqScore column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('WrittenSubmissions') 
    AND name = 'McqScore'
)
BEGIN
    ALTER TABLE WrittenSubmissions
    ADD McqScore DECIMAL(10,2) NULL;
    PRINT 'Added McqScore column';
END
ELSE
BEGIN
    PRINT 'McqScore column already exists';
END
GO

-- Add McqTotalMarks column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID('WrittenSubmissions') 
    AND name = 'McqTotalMarks'
)
BEGIN
    ALTER TABLE WrittenSubmissions
    ADD McqTotalMarks DECIMAL(10,2) NULL;
    PRINT 'Added McqTotalMarks column';
END
ELSE
BEGIN
    PRINT 'McqTotalMarks column already exists';
END
GO

PRINT 'Migration complete: MCQ scores can now be stored in WrittenSubmissions table';
