-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- Lösa pass sparas som pass i en dold split per användare (Split.IsFreeSessions = 1).
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Split', N'IsFreeSessions') IS NULL
    ALTER TABLE dbo.Split ADD IsFreeSessions BIT NOT NULL CONSTRAINT DF_Split_IsFreeSessions DEFAULT ((0));

COMMIT TRANSACTION;
GO

-- Egen batch: kolumnen måste finnas innan det filtrerade indexet kompileras.
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Split_FreeSessions_UserId' AND object_id = OBJECT_ID(N'dbo.Split'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Split_FreeSessions_UserId
        ON dbo.Split (UserId)
        WHERE IsFreeSessions = 1;
