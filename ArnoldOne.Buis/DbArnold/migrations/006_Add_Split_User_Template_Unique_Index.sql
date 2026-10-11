-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- En användare kan bara ha en split per mall (avaktiverade räknas, de återaktiveras).
SET XACT_ABORT ON;
-- Filtrerade index kräver QUOTED_IDENTIFIER ON (standard i SSMS, men inte i sqlcmd).
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF EXISTS
(
    SELECT 1 FROM dbo.Split WHERE SplitTemplateId IS NOT NULL
    GROUP BY UserId, SplitTemplateId HAVING COUNT(*) > 1
)
BEGIN
    -- Dubbletter skapade av dubbelklick. Dölj eller flytta dem för hand först och kör sedan om skriptet.
    SELECT UserId, SplitTemplateId, COUNT(*) AS Antal, MIN(SplitId) AS ForstaSplitId
    FROM dbo.Split WHERE SplitTemplateId IS NOT NULL
    GROUP BY UserId, SplitTemplateId HAVING COUNT(*) > 1;
    THROW 50002, N'Dubbla Split-rader per användare och mall finns (se resultatet ovan). Åtgärda dem innan indexet skapas.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_Split_UserId_SplitTemplateId'
      AND object_id = OBJECT_ID(N'dbo.Split')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Split_UserId_SplitTemplateId
        ON dbo.Split (UserId, SplitTemplateId)
        WHERE SplitTemplateId IS NOT NULL;
END;

COMMIT TRANSACTION;
