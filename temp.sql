CREATE TABLE [Candidates] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(150) NOT NULL,
    [Phone] nvarchar(30) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Candidates] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Jobs] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Company] nvarchar(200) NOT NULL,
    [Location] nvarchar(max) NULL,
    [EmploymentType] nvarchar(max) NULL,
    [RequiredSkillsJson] nvarchar(max) NULL,
    [PreferredSkillsJson] nvarchar(max) NULL,
    [StructuredJson] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ChatSessions] (
    [Id] uniqueidentifier NOT NULL,
    [CandidateId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatSessions_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [FaceEmbeddings] (
    [CandidateId] uniqueidentifier NOT NULL,
    [Embedding] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    CONSTRAINT [PK_FaceEmbeddings] PRIMARY KEY ([CandidateId]),
    CONSTRAINT [FK_FaceEmbeddings_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Resumes] (
    [Id] uniqueidentifier NOT NULL,
    [CandidateId] uniqueidentifier NOT NULL,
    [FileName] nvarchar(260) NOT NULL,
    [OriginalFileName] nvarchar(260) NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [StoragePath] nvarchar(max) NOT NULL,
    [VersionNumber] int NOT NULL,
    [IsLatest] bit NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Resumes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Resumes_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ChatMessages] (
    [Id] uniqueidentifier NOT NULL,
    [ChatSessionId] uniqueidentifier NOT NULL,
    [Sender] int NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChatMessages_ChatSessions_ChatSessionId] FOREIGN KEY ([ChatSessionId]) REFERENCES [ChatSessions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Applications] (
    [Id] uniqueidentifier NOT NULL,
    [CandidateId] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [ResumeId] uniqueidentifier NULL,
    [Status] int NOT NULL,
    [AppliedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Applications_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Applications_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Applications_Resumes_ResumeId] FOREIGN KEY ([ResumeId]) REFERENCES [Resumes] ([Id])
);
GO


CREATE TABLE [AtsAnalyses] (
    [Id] uniqueidentifier NOT NULL,
    [ResumeId] uniqueidentifier NOT NULL,
    [CandidateId] uniqueidentifier NOT NULL,
    [OverallScore] int NOT NULL,
    [CategoryScoresJson] nvarchar(max) NULL,
    [SkillsIdentifiedJson] nvarchar(max) NULL,
    [KeywordsIdentifiedJson] nvarchar(max) NULL,
    [MissingKeywordsJson] nvarchar(max) NULL,
    [MissingSkillsJson] nvarchar(max) NULL,
    [IssuesJson] nvarchar(max) NULL,
    [RecommendationsJson] nvarchar(max) NULL,
    [AnalyzedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AtsAnalyses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AtsAnalyses_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]),
    CONSTRAINT [FK_AtsAnalyses_Resumes_ResumeId] FOREIGN KEY ([ResumeId]) REFERENCES [Resumes] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [MatchResults] (
    [Id] uniqueidentifier NOT NULL,
    [CandidateId] uniqueidentifier NOT NULL,
    [ResumeId] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [MatchScore] int NOT NULL,
    [MatchingSkillsJson] nvarchar(max) NULL,
    [MissingSkillsJson] nvarchar(max) NULL,
    [MatchingKeywordsJson] nvarchar(max) NULL,
    [MissingKeywordsJson] nvarchar(max) NULL,
    [ReasonsJson] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MatchResults] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MatchResults_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [Candidates] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MatchResults_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MatchResults_Resumes_ResumeId] FOREIGN KEY ([ResumeId]) REFERENCES [Resumes] ([Id])
);
GO


CREATE UNIQUE INDEX [IX_Applications_CandidateId_JobId] ON [Applications] ([CandidateId], [JobId]);
GO


CREATE INDEX [IX_Applications_JobId] ON [Applications] ([JobId]);
GO


CREATE INDEX [IX_Applications_ResumeId] ON [Applications] ([ResumeId]);
GO


CREATE INDEX [IX_AtsAnalyses_CandidateId] ON [AtsAnalyses] ([CandidateId]);
GO


CREATE INDEX [IX_AtsAnalyses_ResumeId] ON [AtsAnalyses] ([ResumeId]);
GO


CREATE UNIQUE INDEX [IX_Candidates_Email] ON [Candidates] ([Email]);
GO


CREATE INDEX [IX_ChatMessages_ChatSessionId] ON [ChatMessages] ([ChatSessionId]);
GO


CREATE INDEX [IX_ChatSessions_CandidateId] ON [ChatSessions] ([CandidateId]);
GO


CREATE INDEX [IX_Jobs_IsActive] ON [Jobs] ([IsActive]);
GO


CREATE INDEX [IX_MatchResults_CandidateId_JobId_ResumeId] ON [MatchResults] ([CandidateId], [JobId], [ResumeId]);
GO


CREATE INDEX [IX_MatchResults_JobId] ON [MatchResults] ([JobId]);
GO


CREATE INDEX [IX_MatchResults_ResumeId] ON [MatchResults] ([ResumeId]);
GO


CREATE INDEX [IX_Resumes_CandidateId_IsLatest] ON [Resumes] ([CandidateId], [IsLatest]);
GO


CREATE UNIQUE INDEX [IX_Resumes_CandidateId_VersionNumber] ON [Resumes] ([CandidateId], [VersionNumber]);
GO


