-- MCGCareWEBQI bridge database schema.
-- Idempotent: safe to re-run.
-- Target: SQL Server 2019+ (works on LocalDB).

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IntegrationTransaction')
BEGIN
    CREATE TABLE dbo.IntegrationTransaction
    (
        TransactionId        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_IntegrationTransaction PRIMARY KEY,
        CallerId             NVARCHAR(64)     NOT NULL,
        CallerTxnId          NVARCHAR(128)    NOT NULL,
        Status               NVARCHAR(32)     NOT NULL,
        RequestParamsJson    NVARCHAR(MAX)    NULL,
        OutboundFieldsJson   NVARCHAR(MAX)    NULL,
        McgResponseXml       NVARCHAR(MAX)    NULL,
        McgResponseJson      NVARCHAR(MAX)    NULL,
        McgErrorXml          NVARCHAR(MAX)    NULL,
        CallbackUrl          NVARCHAR(512)    NULL,
        CallbackDeliveredAt  DATETIME2        NULL,
        CallbackAttempts     INT              NOT NULL CONSTRAINT DF_IT_CallbackAttempts DEFAULT 0,
        ReturnContext        NVARCHAR(MAX)    NULL,
        FailureReason        NVARCHAR(MAX)    NULL,
        CreatedAt            DATETIME2        NOT NULL CONSTRAINT DF_IT_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt            DATETIME2        NOT NULL CONSTRAINT DF_IT_UpdatedAt DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_IT_Caller     ON dbo.IntegrationTransaction (CallerId, CallerTxnId);
    CREATE INDEX IX_IT_Status     ON dbo.IntegrationTransaction (Status);
    CREATE INDEX IX_IT_CreatedAt  ON dbo.IntegrationTransaction (CreatedAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IntegrationAudit')
BEGIN
    CREATE TABLE dbo.IntegrationAudit
    (
        AuditId        BIGINT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_IntegrationAudit PRIMARY KEY,
        TransactionId  UNIQUEIDENTIFIER NOT NULL,
        EventType      NVARCHAR(64)     NOT NULL,
        PayloadJson    NVARCHAR(MAX)    NULL,
        CreatedAt      DATETIME2        NOT NULL CONSTRAINT DF_IA_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_IA_Transaction FOREIGN KEY (TransactionId)
            REFERENCES dbo.IntegrationTransaction (TransactionId) ON DELETE CASCADE
    );

    CREATE INDEX IX_IA_TransactionId ON dbo.IntegrationAudit (TransactionId);
    CREATE INDEX IX_IA_CreatedAt     ON dbo.IntegrationAudit (CreatedAt);
END
GO
