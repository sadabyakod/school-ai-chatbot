-- Migration: Add RubricBlobPath to SubjectiveRubrics
-- Adds a nullable NVARCHAR(500) column to store blob URL/path for frozen rubric JSON.
-- Safe to run multiple times; will only add the column if it doesn't exist.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE Name = N'RubricBlobPath'
      AND Object_ID = Object_ID(N'dbo.SubjectiveRubrics')
)
BEGIN
    ALTER TABLE dbo.SubjectiveRubrics
    ADD RubricBlobPath NVARCHAR(500) NULL;

    PRINT 'Added column RubricBlobPath to dbo.SubjectiveRubrics';
END
ELSE
BEGIN
    PRINT 'Column RubricBlobPath already exists on dbo.SubjectiveRubrics. No action taken.';
END
