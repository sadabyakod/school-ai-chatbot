IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Schools] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [ContactInfo] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Website] nvarchar(max) NOT NULL,
    [FeeStructure] nvarchar(max) NOT NULL,
    [Timetable] nvarchar(max) NOT NULL,
    [Holidays] nvarchar(max) NOT NULL,
    [Events] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Schools] PRIMARY KEY ([Id])
);

CREATE TABLE [UploadedFiles] (
    [Id] int NOT NULL IDENTITY,
    [FileName] nvarchar(max) NOT NULL,
    [FilePath] nvarchar(max) NOT NULL,
    [UploadDate] datetime2 NOT NULL,
    [EmbeddingDimension] int NOT NULL,
    [EmbeddingVector] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_UploadedFiles] PRIMARY KEY ([Id])
);

CREATE TABLE [Faqs] (
    [Id] int NOT NULL IDENTITY,
    [Question] nvarchar(max) NOT NULL,
    [Answer] nvarchar(max) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SchoolId] int NOT NULL,
    CONSTRAINT [PK_Faqs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Faqs_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Role] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SchoolId] int NOT NULL,
    [LanguagePreference] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [SyllabusChunks] (
    [Id] int NOT NULL IDENTITY,
    [Subject] nvarchar(max) NOT NULL,
    [Grade] nvarchar(max) NOT NULL,
    [Source] nvarchar(max) NOT NULL,
    [ChunkText] nvarchar(max) NOT NULL,
    [Chapter] nvarchar(max) NOT NULL,
    [UploadedFileId] int NOT NULL,
    [PineconeVectorId] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_SyllabusChunks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SyllabusChunks_UploadedFiles_UploadedFileId] FOREIGN KEY ([UploadedFileId]) REFERENCES [UploadedFiles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [ChatLogs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Response] nvarchar(max) NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [SchoolId] int NOT NULL,
    CONSTRAINT [PK_ChatLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatLogs_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ChatLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Embeddings] (
    [Id] int NOT NULL IDENTITY,
    [SyllabusChunkId] int NOT NULL,
    [VectorJson] nvarchar(max) NOT NULL,
    [SchoolId] int NOT NULL,
    CONSTRAINT [PK_Embeddings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Embeddings_Schools_SchoolId] FOREIGN KEY ([SchoolId]) REFERENCES [Schools] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Embeddings_SyllabusChunks_SyllabusChunkId] FOREIGN KEY ([SyllabusChunkId]) REFERENCES [SyllabusChunks] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_ChatLogs_SchoolId] ON [ChatLogs] ([SchoolId]);

CREATE INDEX [IX_ChatLogs_UserId] ON [ChatLogs] ([UserId]);

CREATE INDEX [IX_Embeddings_SchoolId] ON [Embeddings] ([SchoolId]);

CREATE INDEX [IX_Embeddings_SyllabusChunkId] ON [Embeddings] ([SyllabusChunkId]);

CREATE INDEX [IX_Faqs_SchoolId] ON [Faqs] ([SchoolId]);

CREATE INDEX [IX_SyllabusChunks_UploadedFileId] ON [SyllabusChunks] ([UploadedFileId]);

CREATE INDEX [IX_Users_SchoolId] ON [Users] ([SchoolId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251028191833_InitialCreateForAzureSQL', N'9.0.10');

COMMIT;
GO

