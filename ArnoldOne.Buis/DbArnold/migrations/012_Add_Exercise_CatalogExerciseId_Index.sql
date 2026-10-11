-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- Historik per katalogövning slår upp användarens övningsrader via CatalogExerciseId.
SET QUOTED_IDENTIFIER ON;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Exercise_CatalogExerciseId' AND object_id = OBJECT_ID(N'dbo.Exercise'))
    CREATE NONCLUSTERED INDEX IX_Exercise_CatalogExerciseId
        ON dbo.Exercise (CatalogExerciseId) INCLUDE (PassId)
        WHERE CatalogExerciseId IS NOT NULL;
