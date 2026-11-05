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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [Company] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(30) NOT NULL,
        [RegistrationNumber] nvarchar(50) NOT NULL,
        [TaxNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(50) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [LogoStorageKey] nvarchar(256) NULL,
        [DefaultCurrency] nvarchar(3) NULL,
        [DefaultQuotationTerms] nvarchar(max) NULL,
        [DefaultValidityDays] int NOT NULL,
        [CreationDate] datetime NOT NULL,
        [LastUpdated] datetime NULL,
        CONSTRAINT [PK_Company] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ServiceCategory] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreationDate] datetime NOT NULL,
        [LastUpdated] datetime NULL,
        CONSTRAINT [PK_ServiceCategory] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [Client] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Email] nvarchar(50) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [Address] nvarchar(50) NOT NULL,
        [CustomerTypeId] int NOT NULL,
        [NotesJson] text NULL,
        [IsActive] bit NOT NULL,
        [CreationDate] datetime NOT NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Client] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ClientRequest] (
        [Id] uniqueidentifier NOT NULL,
        [ClientId] uniqueidentifier NOT NULL,
        [ReferenceNumber] nvarchar(50) NULL,
        [Title] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [SiteAddress] nvarchar(50) NOT NULL,
        [PropertyType] nvarchar(50) NULL,
        [StatusId] int NOT NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        [ConfirmedBy] uniqueidentifier NULL,
        [ConfirmationDate] datetime NULL,
        [CreationDate] datetime NOT NULL,
        CONSTRAINT [PK_ClientRequest] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ClientRequest_Client] FOREIGN KEY ([ClientId]) REFERENCES [Client] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [Currency] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(80) NOT NULL,
        [Code] nvarchar(3) NOT NULL,
        [Symbol] nvarchar(10) NOT NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        [CreationDate] datetime2(3) NOT NULL CONSTRAINT [DF_Currency_CreationDate] DEFAULT (SYSUTCDATETIME()),
        [UserId] uniqueidentifier NULL,
        CONSTRAINT [PK_Currency] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_Currency_Code] UNIQUE ([Code])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [Quotation] (
        [Id] uniqueidentifier NOT NULL,
        [RequestId] uniqueidentifier NOT NULL,
        [QuotationNumber] nvarchar(50) NOT NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Quotation] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quotation_ClientRequest] FOREIGN KEY ([RequestId]) REFERENCES [ClientRequest] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [QuotationItem] (
        [Id] uniqueidentifier NOT NULL,
        [QuotationRevisionId] uniqueidentifier NOT NULL,
        [RequestId] uniqueidentifier NOT NULL,
        [ScopeItemId] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [ServiceCodeSnapshot] nvarchar(50) NOT NULL,
        [Title] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Quantity] int NULL,
        [UnitCodeSnapshot] nvarchar(50) NULL,
        [LineAmount] decimal(18,2) NULL,
        [RequiresSiteAssessment] bit NOT NULL,
        [AssessmentNotesSnapshot] nvarchar(50) NULL,
        [WorkStagesSnapshotJson] text NULL,
        [DependenciesSnapshotJson] text NULL,
        CONSTRAINT [PK_QuotationItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuotationItem_ClientRequest] FOREIGN KEY ([RequestId]) REFERENCES [ClientRequest] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [QuotationRevision] (
        [Id] uniqueidentifier NOT NULL,
        [QuotationId] uniqueidentifier NOT NULL,
        [RevisionNumber] int NOT NULL,
        [StatusId] int NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [CompanyNameSnapshot] nvarchar(150) NOT NULL,
        [CompanyEmailSnapshot] nvarchar(256) NULL,
        [CompanyPhoneSnapshot] nvarchar(40) NULL,
        [CompanyAddressSnapshot] nvarchar(500) NULL,
        [CompanyRegistrationNumberSnapshot] nvarchar(80) NULL,
        [CompanyTaxNumberSnapshot] nvarchar(80) NULL,
        [CompanyLogoStorageKeySnapshot] nvarchar(500) NULL,
        [CustomerNameSnapshot] nvarchar(150) NOT NULL,
        [CustomerEmailSnapshot] nvarchar(256) NULL,
        [CustomerPhoneSnapshot] nvarchar(40) NULL,
        [CustomerAddressSnapshot] nvarchar(500) NULL,
        [SiteAddressSnapshot] nvarchar(500) NOT NULL,
        [RequestDescriptionSnapshot] nvarchar(max) NOT NULL,
        [ClarificationsSnapshotJson] nvarchar(max) NULL,
        [Terms] nvarchar(4000) NULL,
        [Exclusions] nvarchar(4000) NULL,
        [IssueDate] datetime NULL,
        [ValidUntil] datetime NULL,
        [IncludesPrices] bit NOT NULL CONSTRAINT [DF_QuotationRevision_IncludesPrices] DEFAULT CAST(1 AS bit),
        [CurrencyCode] nvarchar(3) NULL,
        [TaxTreatmentId] int NULL CONSTRAINT [DF_QuotationRevision_TaxTreatment] DEFAULT 0,
        [Subtotal] decimal(18,2) NULL,
        [TaxAmount] decimal(18,2) NULL,
        [Total] decimal(18,2) NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        [CreationDate] datetime NOT NULL CONSTRAINT [DF_QuotationRevision_CreatedAtUtc] DEFAULT ((sysutcdatetime())),
        [LastUpdate] datetime NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovalDate] datetime NULL,
        [ApprovedDocumentJson] nvarchar(max) NULL,
        CONSTRAINT [PK_QuotationRevision] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuotationRevision_Quotation] FOREIGN KEY ([QuotationId]) REFERENCES [Quotation] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ScopeClause] (
        [Id] uniqueidentifier NOT NULL,
        [ScopeItemId] uniqueidentifier NOT NULL,
        [ClauseTypeId] int NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreationDate] datetime NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ScopeClause] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ScopeDependency] (
        [Id] uniqueidentifier NOT NULL,
        [RequestId] uniqueidentifier NOT NULL,
        [ScopeItemId] uniqueidentifier NOT NULL,
        [DependsOnScopeItemId] uniqueidentifier NOT NULL,
        [NotesJson] text NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        [CreationDate] datetime NOT NULL,
        CONSTRAINT [PK_ScopeDependency] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScopeDependency_ClientRequest] FOREIGN KEY ([RequestId]) REFERENCES [ClientRequest] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ScopeItem] (
        [Id] uniqueidentifier NOT NULL,
        [RequestId] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [ServiceCodeSnapshot] nvarchar(50) NOT NULL,
        [Title] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Quantity] int NULL,
        [UnitCodeSnapshot] nvarchar(50) NULL,
        [RequireAssessment] bit NOT NULL,
        [AssessmentNotesJson] text NULL,
        [SourceId] int NULL,
        [ReviewStatusId] int NOT NULL,
        [ReviewedByUserId] uniqueidentifier NOT NULL,
        [ReviewedOn] datetime NULL,
        CONSTRAINT [PK_ScopeItem] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScopeItem_ClientRequest] FOREIGN KEY ([RequestId]) REFERENCES [ClientRequest] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ScopeStage] (
        [Id] uniqueidentifier NOT NULL,
        [ScopeItemId] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreationDate] datetime NOT NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ScopeStage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScopeStage_ScopeItem] FOREIGN KEY ([ScopeItemId]) REFERENCES [ScopeItem] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [Service] (
        [Id] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        [RequireSiteAssessment] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreationDate] datetime NOT NULL,
        [LastUpdated] datetime NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Service] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Service_ServiceCategory] FOREIGN KEY ([CategoryId]) REFERENCES [ServiceCategory] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ServiceClause] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [ClauseTypeId] int NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_ServiceClause] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceClause_Service] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ServiceDependency] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NOT NULL,
        [DependsOnServiceId] uniqueidentifier NOT NULL,
        [CreationDate] datetime NOT NULL,
        [CreatorId] uniqueidentifier NULL,
        CONSTRAINT [PK_ServiceDependency] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceDependency_Service] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id]),
        CONSTRAINT [FK_ServiceDependency_Service1] FOREIGN KEY ([DependsOnServiceId]) REFERENCES [Service] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ServiceStage] (
        [Id] uniqueidentifier NOT NULL,
        [ServiceId] uniqueidentifier NULL,
        [Name] nvarchar(50) NULL,
        [Desrciption] nvarchar(max) NULL,
        [CreationDate] datetime NULL,
        [CreatorId] uniqueidentifier NULL,
        CONSTRAINT [PK_ServiceStage] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceStage_Service] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [ServiceUnit] (
        [Id] uniqueidentifier NOT NULL,
        [UnitId] uniqueidentifier NULL,
        [ServiceId] uniqueidentifier NULL,
        [IsDefault] bit NULL,
        CONSTRAINT [PK_ServiceUnit] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ServiceUnit_Service] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [Unit] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreationDate] datetime NOT NULL,
        [LastUpdated] datetime NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Unit] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [User] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NULL,
        [Surname] nvarchar(50) NULL,
        [Email] nvarchar(50) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [IsActive] bit NOT NULL,
        [IsEmailConfirmed] bit NOT NULL,
        [GroupId] uniqueidentifier NULL,
        [PasswordHash] nvarchar(256) NOT NULL,
        [SecurityStamp] nvarchar(50) NULL,
        [AuthenticationKey] nvarchar(50) NULL,
        [AuthRecoveryCodes] nvarchar(50) NULL,
        [TwoFactorAuthEnabled] bit NULL,
        [LastLoginDate] datetime NULL,
        [AccessFailedCount] int NULL,
        [ActivationDate] datetime NULL,
        [CreationDate] datetime NOT NULL,
        [LastUpdated] datetime NULL,
        [CreatorId] uniqueidentifier NULL,
        CONSTRAINT [PK_User] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_User_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [UserGroup] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(50) NOT NULL,
        [RightsId] int NOT NULL,
        [CreationDate] datetime NOT NULL,
        [CreatorId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserGroup] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserGroup_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE TABLE [UserSession] (
        [UserId] uniqueidentifier NOT NULL,
        [LoginDate] datetime NOT NULL,
        [LockoutDate] datetime NULL,
        [ExpiryDate] datetime NULL,
        CONSTRAINT [PK_UserSession] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_UserSession_User] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Client_CreatorId] ON [Client] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ClientRequest_ClientId] ON [ClientRequest] ([ClientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ClientRequest_ConfirmedBy] ON [ClientRequest] ([ConfirmedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ClientRequest_CreatorId] ON [ClientRequest] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Currency_CreatorId] ON [Currency] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Currency_UserId] ON [Currency] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Currency_Code] ON [Currency] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Quotation_CreatorId] ON [Quotation] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Quotation_RequestId] ON [Quotation] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_QuotationItem_QuotationRevisionId] ON [QuotationItem] ([QuotationRevisionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_QuotationItem_RequestId] ON [QuotationItem] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_QuotationItem_ScopeItemId] ON [QuotationItem] ([ScopeItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_QuotationItem_ServiceId] ON [QuotationItem] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_QuotationRevision_ApprovedByUserId] ON [QuotationRevision] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_QuotationRevision_CreatedByUserId] ON [QuotationRevision] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_QuotationRevision_Number] ON [QuotationRevision] ([QuotationId], [RevisionNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeClause_CreatorId] ON [ScopeClause] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeClause_ScopeItemId] ON [ScopeClause] ([ScopeItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeDependency_CreatorId] ON [ScopeDependency] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeDependency_DependsOnScopeItemId] ON [ScopeDependency] ([DependsOnScopeItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeDependency_RequestId] ON [ScopeDependency] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeDependency_ScopeItemId] ON [ScopeDependency] ([ScopeItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeItem_RequestId] ON [ScopeItem] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeItem_ReviewedByUserId] ON [ScopeItem] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeItem_ServiceId] ON [ScopeItem] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeStage_CreatorId] ON [ScopeStage] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ScopeStage_ScopeItemId] ON [ScopeStage] ([ScopeItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Service_CategoryId] ON [Service] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Service_CreatorId] ON [Service] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceClause_ServiceId] ON [ServiceClause] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceDependency_CreatorId] ON [ServiceDependency] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceDependency_DependsOnServiceId] ON [ServiceDependency] ([DependsOnServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceDependency_ServiceId] ON [ServiceDependency] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceStage_CreatorId] ON [ServiceStage] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceStage_ServiceId] ON [ServiceStage] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceUnit_ServiceId] ON [ServiceUnit] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_ServiceUnit_UnitId] ON [ServiceUnit] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_Unit_CreatorId] ON [Unit] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_User_CreatorId] ON [User] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_User_GroupId] ON [User] ([GroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    CREATE INDEX [IX_UserGroup_CreatorId] ON [UserGroup] ([CreatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [Client] ADD CONSTRAINT [FK_Client_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ClientRequest] ADD CONSTRAINT [FK_ClientRequest_User] FOREIGN KEY ([ConfirmedBy]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ClientRequest] ADD CONSTRAINT [FK_ClientRequest_User1] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [Currency] ADD CONSTRAINT [FK_Currency_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [Currency] ADD CONSTRAINT [FK_Currency_User_UserId] FOREIGN KEY ([UserId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [Quotation] ADD CONSTRAINT [FK_Quotation_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [QuotationItem] ADD CONSTRAINT [FK_QuotationItem_QuotationRevision] FOREIGN KEY ([QuotationRevisionId]) REFERENCES [QuotationRevision] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [QuotationItem] ADD CONSTRAINT [FK_QuotationItem_ScopeItem] FOREIGN KEY ([ScopeItemId]) REFERENCES [ScopeItem] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [QuotationItem] ADD CONSTRAINT [FK_QuotationItem_Service] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [QuotationRevision] ADD CONSTRAINT [FK_QuotationRevision_ApprovedByUser] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [QuotationRevision] ADD CONSTRAINT [FK_QuotationRevision_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeClause] ADD CONSTRAINT [FK_ScopeClause_ScopeItem] FOREIGN KEY ([ScopeItemId]) REFERENCES [ScopeItem] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeClause] ADD CONSTRAINT [FK_ScopeClause_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeDependency] ADD CONSTRAINT [FK_ScopeDependency_ScopeItem] FOREIGN KEY ([ScopeItemId]) REFERENCES [ScopeItem] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeDependency] ADD CONSTRAINT [FK_ScopeDependency_ScopeItem1] FOREIGN KEY ([DependsOnScopeItemId]) REFERENCES [ScopeItem] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeDependency] ADD CONSTRAINT [FK_ScopeDependency_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeItem] ADD CONSTRAINT [FK_ScopeItem_Service] FOREIGN KEY ([ServiceId]) REFERENCES [Service] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeItem] ADD CONSTRAINT [FK_ScopeItem_User] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ScopeStage] ADD CONSTRAINT [FK_ScopeStage_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [Service] ADD CONSTRAINT [FK_Service_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ServiceDependency] ADD CONSTRAINT [FK_ServiceDependency_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ServiceStage] ADD CONSTRAINT [FK_ServiceStage_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [ServiceUnit] ADD CONSTRAINT [FK_ServiceUnit_Unit] FOREIGN KEY ([UnitId]) REFERENCES [Unit] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [Unit] ADD CONSTRAINT [FK_Unit_User] FOREIGN KEY ([CreatorId]) REFERENCES [User] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    ALTER TABLE [User] ADD CONSTRAINT [FK_User_UserGroup] FOREIGN KEY ([GroupId]) REFERENCES [UserGroup] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104203545_adding creatorId in User'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104203545_adding creatorId in User', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104204158_adding currency table'
)
BEGIN
    ALTER TABLE [Currency] DROP CONSTRAINT [FK_Currency_User_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104204158_adding currency table'
)
BEGIN
    ALTER TABLE [Currency] DROP CONSTRAINT [UQ_Currency_Code];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104204158_adding currency table'
)
BEGIN
    DROP INDEX [IX_Currency_UserId] ON [Currency];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104204158_adding currency table'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Currency]') AND [c].[name] = N'UserId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Currency] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Currency] DROP COLUMN [UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104204158_adding currency table'
)
BEGIN
    ALTER TABLE [Currency] DROP CONSTRAINT [DF_Currency_CreationDate];
    ALTER TABLE [Currency] ALTER COLUMN [CreationDate] datetime NOT NULL;
    ALTER TABLE [Currency] ADD CONSTRAINT [DF_Currency_CreationDate] DEFAULT ((sysutcdatetime())) FOR [CreationDate];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104204158_adding currency table'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104204158_adding currency table', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105010000_RestoreCurrencyCodeUniqueness'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Currency_Code] ON [Currency] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105010000_RestoreCurrencyCodeUniqueness'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105010000_RestoreCurrencyCodeUniqueness', N'10.0.12');
END;

COMMIT;
GO

