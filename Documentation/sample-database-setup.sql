-- Sample SQL Script to create and populate Images table
-- Run this against your SQL Server database

USE [YourDatabaseName];
GO

-- Create Images table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Images' and xtype='U')
BEGIN
    CREATE TABLE Images (
        Id int IDENTITY(1,1) PRIMARY KEY,
        ImagePath nvarchar(500) NOT NULL,
        ImageName nvarchar(255),
        Description nvarchar(500),
        CreatedDate datetime2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate datetime2,
        ContentType nvarchar(50),
        FileSize bigint
    );

    -- Create indexes for better performance
    CREATE NONCLUSTERED INDEX IX_Images_ImageName ON Images (ImageName);
    CREATE NONCLUSTERED INDEX IX_Images_CreatedDate ON Images (CreatedDate);
END
GO

-- Insert sample data (update paths to actual image locations)
INSERT INTO Images (ImagePath, ImageName, Description, ContentType, FileSize)
VALUES 
    ('C:\Images\logo.png', 'Company Logo', 'Main company logo', 'image/png', 45120),
    ('C:\Images\banner.jpg', 'Website Banner', 'Header banner for website', 'image/jpeg', 128456),
    ('C:\Images\profile.jpg', 'Default Profile', 'Default user profile image', 'image/jpeg', 67890),
    ('C:\Images\background.png', 'App Background', 'Application background image', 'image/png', 234567);
GO

-- Query to verify data
SELECT Id, ImageName, Description, CreatedDate, ContentType, FileSize 
FROM Images 
ORDER BY CreatedDate DESC;
GO