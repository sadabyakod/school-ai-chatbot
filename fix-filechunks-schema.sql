-- Fix FileChunks table schema to match Entity Framework model
-- Run this on your Azure SQL database

PRINT 'Checking FileChunks table schema...'

-- Check if FileChunks table exists
IF OBJECT_ID('dbo.FileChunks', 'U') IS NOT NULL
BEGIN
    PRINT 'FileChunks table exists. Checking columns...'
    
    -- Add FileId column if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FileChunks') AND name = 'FileId')
    BEGIN
        PRINT 'Adding FileId column...'
        ALTER TABLE dbo.FileChunks ADD FileId INT NOT NULL DEFAULT 0
    END
    
    -- Add Chapter column if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FileChunks') AND name = 'Chapter')
    BEGIN
        PRINT 'Adding Chapter column...'
        ALTER TABLE dbo.FileChunks ADD Chapter NVARCHAR(200) NULL
    END
    
    -- Add ChunkIndex column if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FileChunks') AND name = 'ChunkIndex')
    BEGIN
        PRINT 'Adding ChunkIndex column...'
        ALTER TABLE dbo.FileChunks ADD ChunkIndex INT NOT NULL DEFAULT 0
    END
    
    -- Add CreatedAt column if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FileChunks') AND name = 'CreatedAt')
    BEGIN
        PRINT 'Adding CreatedAt column...'
        ALTER TABLE dbo.FileChunks ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    END
    
    -- Add Grade column if missing  
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FileChunks') AND name = 'Grade')
    BEGIN
        PRINT 'Adding Grade column...'
        ALTER TABLE dbo.FileChunks ADD Grade NVARCHAR(50) NULL
    END
    
    -- Add Subject column if missing
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FileChunks') AND name = 'Subject')
    BEGIN
        PRINT 'Adding Subject column...'
        ALTER TABLE dbo.FileChunks ADD Subject NVARCHAR(100) NULL
    END
    
    PRINT 'FileChunks schema update completed!'
END
ELSE
BEGIN
    PRINT 'FileChunks table does not exist. Please run Entity Framework migrations first.'
END

PRINT 'Done!'
GO
