-- Kör i Tyngd-databasen med ett konto som har ALTER-behörighet.
-- Hindrar dubbla övningsloggar när två sparningar av samma pass sker samtidigt.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS
(
    SELECT 1 FROM dbo.ExerciseLog GROUP BY SessionId, ExerciseId HAVING COUNT(*) > 1
)
BEGIN
    -- Slå ihop eller ta bort dubbletterna (och deras SetLog) för hand först, och kör sedan om skriptet.
    SELECT SessionId, ExerciseId, COUNT(*) AS Antal
    FROM dbo.ExerciseLog GROUP BY SessionId, ExerciseId HAVING COUNT(*) > 1;
    THROW 50001, N'Dubbla ExerciseLog-rader finns (se resultatet ovan). Åtgärda dem innan indexet skapas.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'UX_ExerciseLog_SessionId_ExerciseId'
      AND object_id = OBJECT_ID(N'dbo.ExerciseLog')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ExerciseLog_SessionId_ExerciseId
        ON dbo.ExerciseLog (SessionId, ExerciseId);
END;

COMMIT TRANSACTION;
