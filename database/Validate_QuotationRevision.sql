USE [Build-Wise];

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

-- Keep 0 to create the table. 1 tests everything and rolls it back.
DECLARE @ValidationOnly bit = 1;

IF @@TRANCOUNT <> 0
BEGIN
    RAISERROR ('Run this script in a query window with no open transaction.', 16, 1);
    RETURN;
END;

IF OBJECT_ID(N'dbo.Quotation', N'U') IS NULL
   OR OBJECT_ID(N'dbo.User', N'U') IS NULL
BEGIN
    RAISERROR ('dbo.Quotation and dbo.[User] must exist in Build-Wise on this connection.', 16, 1);
    RETURN;
END;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.QuotationRevision', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.QuotationRevision
        (
            Id uniqueidentifier NOT NULL
                CONSTRAINT DF_QuotationRevision_Id DEFAULT NEWSEQUENTIALID(),
            QuotationId uniqueidentifier NOT NULL,
            RevisionNumber int NOT NULL,
            Status tinyint NOT NULL
                CONSTRAINT DF_QuotationRevision_Status DEFAULT (0),
            Title nvarchar(150) NOT NULL,

            CompanyNameSnapshot nvarchar(150) NOT NULL,
            CompanyEmailSnapshot nvarchar(256) NULL,
            CompanyPhoneSnapshot nvarchar(40) NULL,
            CompanyAddressSnapshot nvarchar(500) NULL,
            CompanyRegistrationNumberSnapshot nvarchar(80) NULL,
            CompanyTaxNumberSnapshot nvarchar(80) NULL,
            CompanyLogoStorageKeySnapshot nvarchar(500) NULL,

            CustomerNameSnapshot nvarchar(150) NOT NULL,
            CustomerEmailSnapshot nvarchar(256) NULL,
            CustomerPhoneSnapshot nvarchar(40) NULL,
            CustomerAddressSnapshot nvarchar(500) NULL,

            SiteAddressSnapshot nvarchar(500) NOT NULL,
            RequestDescriptionSnapshot nvarchar(max) NOT NULL,
            ClarificationsSnapshotJson nvarchar(max) NULL,
            Terms nvarchar(4000) NOT NULL,
            Exclusions nvarchar(4000) NULL,
            IssueDate date NULL,
            ValidUntil date NULL,

            IncludesPrices bit NOT NULL
                CONSTRAINT DF_QuotationRevision_IncludesPrices DEFAULT (1),
            CurrencyCode char(3) NULL,
            TaxTreatment tinyint NOT NULL
                CONSTRAINT DF_QuotationRevision_TaxTreatment DEFAULT (0),
            Subtotal decimal(19,2) NULL,
            TaxAmount decimal(19,2) NULL,
            Total AS
            (
                CASE
                    WHEN IncludesPrices = 1
                         AND Subtotal IS NOT NULL
                         AND TaxAmount IS NOT NULL
                    THEN CONVERT(decimal(19,2), Subtotal + TaxAmount)
                    ELSE NULL
                END
            ) PERSISTED,

            CreatedByUserId uniqueidentifier NOT NULL,
            CreatedAtUtc datetime2(3) NOT NULL
                CONSTRAINT DF_QuotationRevision_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
            UpdatedAtUtc datetime2(3) NULL,
            ApprovedByUserId uniqueidentifier NULL,
            ApprovedAtUtc datetime2(3) NULL,
            ApprovedDocumentJson nvarchar(max) NULL,
            RowVersion rowversion NOT NULL,

            CONSTRAINT PK_QuotationRevision PRIMARY KEY (Id),
            CONSTRAINT UQ_QuotationRevision_Number UNIQUE (QuotationId, RevisionNumber),
            CONSTRAINT CK_QuotationRevision_Number CHECK (RevisionNumber > 0),
            CONSTRAINT CK_QuotationRevision_Status CHECK (Status IN (0, 1)),
            CONSTRAINT CK_QuotationRevision_TaxTreatment CHECK (TaxTreatment IN (0, 1, 2)),
            CONSTRAINT CK_QuotationRevision_Subtotal CHECK (Subtotal IS NULL OR Subtotal >= 0),
            CONSTRAINT CK_QuotationRevision_TaxAmount CHECK (TaxAmount IS NULL OR TaxAmount >= 0),
            CONSTRAINT CK_QuotationRevision_Dates CHECK
            (
                ValidUntil IS NULL
                OR (IssueDate IS NOT NULL AND ValidUntil >= IssueDate)
            ),
            CONSTRAINT CK_QuotationRevision_TaxDetails CHECK
            (
                (TaxTreatment = 0 AND TaxAmount IS NULL)
                OR (TaxTreatment = 1 AND TaxAmount IS NOT NULL AND TaxAmount = 0)
                OR (TaxTreatment = 2 AND TaxAmount IS NOT NULL)
            ),
            CONSTRAINT CK_QuotationRevision_Pricing CHECK
            (
                (
                    IncludesPrices = 0 AND CurrencyCode IS NULL
                    AND Subtotal IS NULL AND TaxAmount IS NULL AND TaxTreatment = 0
                )
                OR
                (
                    IncludesPrices = 1 AND CurrencyCode IS NOT NULL
                    AND LEN(CurrencyCode) = 3
                )
            ),
            CONSTRAINT CK_QuotationRevision_ClarificationsJson CHECK
            (
                ClarificationsSnapshotJson IS NULL
                OR ISJSON(ClarificationsSnapshotJson) = 1
            ),
            CONSTRAINT CK_QuotationRevision_DocumentJson CHECK
            (
                ApprovedDocumentJson IS NULL OR ISJSON(ApprovedDocumentJson) = 1
            ),
            CONSTRAINT CK_QuotationRevision_Approval CHECK
            (
                (
                    Status = 0 AND ApprovedByUserId IS NULL
                    AND ApprovedAtUtc IS NULL AND ApprovedDocumentJson IS NULL
                )
                OR
                (
                    Status = 1 AND IssueDate IS NOT NULL
                    AND ApprovedByUserId IS NOT NULL AND ApprovedAtUtc IS NOT NULL
                    AND ApprovedDocumentJson IS NOT NULL
                    AND (IncludesPrices = 0 OR (Subtotal IS NOT NULL AND TaxAmount IS NOT NULL))
                )
            )
        );
    END;

    -- Add foreign keys after table creation so they bind against current metadata.
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.QuotationRevision')
          AND name = N'FK_QuotationRevision_Quotation'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.QuotationRevision WITH CHECK
            ADD CONSTRAINT FK_QuotationRevision_Quotation
                FOREIGN KEY (QuotationId) REFERENCES dbo.Quotation (Id);';
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.QuotationRevision')
          AND name = N'FK_QuotationRevision_CreatedByUser'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.QuotationRevision WITH CHECK
            ADD CONSTRAINT FK_QuotationRevision_CreatedByUser
                FOREIGN KEY (CreatedByUserId) REFERENCES dbo.[User] (Id);';
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.QuotationRevision')
          AND name = N'FK_QuotationRevision_ApprovedByUser'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.QuotationRevision WITH CHECK
            ADD CONSTRAINT FK_QuotationRevision_ApprovedByUser
                FOREIGN KEY (ApprovedByUserId) REFERENCES dbo.[User] (Id);';
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.QuotationRevision')
          AND name = N'IX_QuotationRevision_CreatedByUserId'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_QuotationRevision_CreatedByUserId
            ON dbo.QuotationRevision (CreatedByUserId);';
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.QuotationRevision')
          AND name = N'IX_QuotationRevision_ApprovedByUserId'
    )
    BEGIN
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_QuotationRevision_ApprovedByUserId
            ON dbo.QuotationRevision (ApprovedByUserId);';
    END;

    DECLARE @TriggerSql nvarchar(max);

    SET @TriggerSql =
        CASE WHEN OBJECT_ID(N'dbo.TR_QuotationRevision_ProtectApproved', N'TR') IS NULL
            THEN N'CREATE' ELSE N'ALTER' END
        + N' TRIGGER dbo.TR_QuotationRevision_ProtectApproved
        ON dbo.QuotationRevision
        AFTER UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS (SELECT 1 FROM deleted WHERE Status = 1)
            BEGIN
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

                RAISERROR (
                    ''Approved revisions cannot be changed. Create a new revision.'',
                    16, 1
                );

                RETURN;
            END;
        END;';

    EXEC sys.sp_executesql @TriggerSql;

    IF @ValidationOnly = 1
    BEGIN
        SELECT name AS ValidatedForeignKey, is_disabled, is_not_trusted
        FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID(N'dbo.QuotationRevision');

        ROLLBACK TRANSACTION;
        PRINT 'Validation succeeded. All test changes were rolled back.';
    END
    ELSE
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'QuotationRevision, its foreign keys, indexes and trigger are ready.';
    END;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage nvarchar(2048) = ERROR_MESSAGE();
    RAISERROR ('QuotationRevision creation failed: %s', 16, 1, @ErrorMessage);
END CATCH;

SELECT OBJECT_ID(N'dbo.QuotationRevision', N'U') AS TableAfterRollback, OBJECT_ID(N'dbo.TR_QuotationRevision_ProtectApproved', N'TR') AS TriggerAfterRollback;

