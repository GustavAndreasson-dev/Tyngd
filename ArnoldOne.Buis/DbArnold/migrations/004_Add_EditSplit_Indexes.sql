-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Pass_SplitId_Visability_Ordning'
      AND object_id = OBJECT_ID(N'dbo.Pass')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Pass_SplitId_Visability_Ordning
        ON dbo.Pass (SplitId, Visability, Ordning);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE [name] = N'IX_Exercise_PassId_Visability_Ordning'
      AND object_id = OBJECT_ID(N'dbo.Exercise')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Exercise_PassId_Visability_Ordning
        ON dbo.Exercise (PassId, Visability, Ordning);
END;

COMMIT TRANSACTION;
