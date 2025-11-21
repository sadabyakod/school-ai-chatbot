-- Update UploadedFiles table schema for Azure Blob Storage integration
-- Backup existing data if any
IF OBJECT_ID('UploadedFiles_Backup', 'U') IS NOT NULL
    DROP TABLE UploadedFiles_Backup;

SELECT * INTO UploadedFiles_Backup FROM UploadedFiles;

-- Drop old columns
ALTER TABLE UploadedFiles DROP COLUMN IF EXISTS FilePath;
ALTER TABLE UploadedFiles DROP COLUMN IF EXISTS UploadDate;
ALTER TABLE UploadedFiles DROP COLUMN IF EXISTS EmbeddingDimension;
ALTER TABLE UploadedFiles DROP COLUMN IF EXISTS EmbeddingVector;

-- Update FileName column
ALTER TABLE UploadedFiles ALTER COLUMN FileName NVARCHAR(500) NOT NULL;

-- Add new columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'BlobUrl')
    ALTER TABLE UploadedFiles ADD BlobUrl NVARCHAR(500) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'UploadedAt')
    ALTER TABLE UploadedFiles ADD UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE();

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'UploadedBy')
    ALTER TABLE UploadedFiles ADD UploadedBy NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Subject')
    ALTER TABLE UploadedFiles ADD Subject NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Grade')
    ALTER TABLE UploadedFiles ADD Grade NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Chapter')
    ALTER TABLE UploadedFiles ADD Chapter NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'TotalChunks')
    ALTER TABLE UploadedFiles ADD TotalChunks INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('UploadedFiles') AND name = 'Status')
    ALTER TABLE UploadedFiles ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Pending';

PRINT 'UploadedFiles table schema updated successfully!';
