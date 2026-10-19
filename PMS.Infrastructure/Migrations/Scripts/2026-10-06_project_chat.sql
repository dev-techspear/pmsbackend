BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    CREATE TABLE [chat_messages] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [SenderId] int NULL,
        [Body] nvarchar(4000) NOT NULL,
        [EditedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_chat_messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_messages_projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_chat_messages_users_SenderId] FOREIGN KEY ([SenderId]) REFERENCES [users] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    CREATE TABLE [chat_read_states] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [UserId] int NOT NULL,
        [LastReadMessageId] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_chat_read_states] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_read_states_projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_chat_read_states_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    CREATE INDEX [IX_chat_messages_ProjectId_Id] ON [chat_messages] ([ProjectId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    CREATE INDEX [IX_chat_messages_SenderId] ON [chat_messages] ([SenderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_read_states_ProjectId_UserId] ON [chat_read_states] ([ProjectId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    CREATE INDEX [IX_chat_read_states_UserId] ON [chat_read_states] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006041343_AddProjectChat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006041343_AddProjectChat', N'9.0.2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006131024_AddChatMentionsAndAttachments'
)
BEGIN
    CREATE TABLE [chat_attachments] (
        [Id] int NOT NULL IDENTITY,
        [MessageId] int NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentType] nvarchar(100) NOT NULL,
        [Size] int NOT NULL,
        [Content] varbinary(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_chat_attachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_attachments_chat_messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [chat_messages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006131024_AddChatMentionsAndAttachments'
)
BEGIN
    CREATE TABLE [chat_mentions] (
        [Id] int NOT NULL IDENTITY,
        [MessageId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_chat_mentions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_chat_mentions_chat_messages_MessageId] FOREIGN KEY ([MessageId]) REFERENCES [chat_messages] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_chat_mentions_users_UserId] FOREIGN KEY ([UserId]) REFERENCES [users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006131024_AddChatMentionsAndAttachments'
)
BEGIN
    CREATE INDEX [IX_chat_attachments_MessageId] ON [chat_attachments] ([MessageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006131024_AddChatMentionsAndAttachments'
)
BEGIN
    CREATE UNIQUE INDEX [IX_chat_mentions_MessageId_UserId] ON [chat_mentions] ([MessageId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006131024_AddChatMentionsAndAttachments'
)
BEGIN
    CREATE INDEX [IX_chat_mentions_UserId] ON [chat_mentions] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261006131024_AddChatMentionsAndAttachments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261006131024_AddChatMentionsAndAttachments', N'9.0.2');
END;

COMMIT;
GO

