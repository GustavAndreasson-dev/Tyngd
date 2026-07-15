-- Kör i Arnold-databasen först efter granskning och godkännande.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Exercise', 'Visability') IS NULL
BEGIN
    ALTER TABLE dbo.Exercise
        ADD Visability BIT NOT NULL
            CONSTRAINT DF_Exercise_Visability DEFAULT (1) WITH VALUES;
END;

IF COL_LENGTH('dbo.Session', 'SessionKey') IS NULL
    ALTER TABLE dbo.Session ADD SessionKey UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.Session', 'StartedAtUtc') IS NULL
    ALTER TABLE dbo.Session ADD StartedAtUtc DATETIMEOFFSET NULL;

IF COL_LENGTH('dbo.Session', 'CompletedAtUtc') IS NULL
    ALTER TABLE dbo.Session ADD CompletedAtUtc DATETIMEOFFSET NULL;

-- SQL Server kompilerar en hel batch innan ALTER TABLE körs. Ny batch krävs
-- innan de nya kolumnerna kan refereras av backfillen.
GO

DECLARE @LegacySessions TABLE (SessionId INT PRIMARY KEY);
INSERT INTO @LegacySessions (SessionId)
SELECT SessionId
FROM dbo.Session
WHERE SessionKey IS NULL OR StartedAtUtc IS NULL;

UPDATE dbo.Session
SET SessionKey = COALESCE(SessionKey, NEWID()),
    StartedAtUtc = COALESCE(StartedAtUtc, TODATETIMEOFFSET(CAST([Date] AS DATETIME2), '+00:00'))
WHERE SessionKey IS NULL
   OR StartedAtUtc IS NULL;

UPDATE target
SET CompletedAtUtc = COALESCE(target.CompletedAtUtc, target.StartedAtUtc)
FROM dbo.Session AS target
INNER JOIN @LegacySessions AS legacy ON legacy.SessionId = target.SessionId;

ALTER TABLE dbo.Session ALTER COLUMN SessionKey UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.Session ALTER COLUMN StartedAtUtc DATETIMEOFFSET NOT NULL;

IF OBJECT_ID('dbo.DF_Session_SessionKey', 'D') IS NULL
    ALTER TABLE dbo.Session
        ADD CONSTRAINT DF_Session_SessionKey DEFAULT (NEWID()) FOR SessionKey;

IF OBJECT_ID('dbo.DF_Session_StartedAtUtc', 'D') IS NULL
    ALTER TABLE dbo.Session
        ADD CONSTRAINT DF_Session_StartedAtUtc DEFAULT (SYSUTCDATETIME()) FOR StartedAtUtc;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Session_SessionKey'
      AND object_id = OBJECT_ID('dbo.Session'))
BEGIN
    CREATE UNIQUE INDEX UX_Session_SessionKey ON dbo.Session(SessionKey);
END;

COMMIT TRANSACTION;
