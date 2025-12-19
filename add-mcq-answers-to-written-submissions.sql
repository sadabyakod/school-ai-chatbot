-- Add McqAnswers column to WrittenSubmissions table
-- This column stores MCQ answers submitted with the answer sheet in JSON format
-- Format: [{"questionId": "Q1", "selectedOption": "A"}]

USE smartstudydb;
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'WrittenSubmissions' 
    AND COLUMN_NAME = 'McqAnswers'
)
BEGIN
    ALTER TABLE WrittenSubmissions
    ADD McqAnswers NVARCHAR(MAX) NULL;
    
    PRINT 'McqAnswers column added to WrittenSubmissions table successfully.';
END
ELSE
BEGIN
    PRINT 'McqAnswers column already exists in WrittenSubmissions table.';
END
GO

-- Verify the column was added
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'WrittenSubmissions'
AND COLUMN_NAME = 'McqAnswers';
GO
