SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.Currency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Currency
    (
        Id uniqueidentifier NOT NULL
            CONSTRAINT PK_Currency PRIMARY KEY,
        Name nvarchar(80) NOT NULL,
        Code nvarchar(3) NOT NULL,
        Symbol nvarchar(10) NOT NULL,
        CreatorId uniqueidentifier NOT NULL,
        CreationDate datetime2(3) NOT NULL
            CONSTRAINT DF_Currency_CreationDate DEFAULT SYSUTCDATETIME(),

        CONSTRAINT UQ_Currency_Code UNIQUE (Code),
        CONSTRAINT CK_Currency_Code CHECK
            (LEN(Code) = 3 AND Code COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[^A-Z]%'),
        CONSTRAINT FK_Currency_User FOREIGN KEY (CreatorId)
            REFERENCES dbo.[User] (Id)
    );
END;
GO
