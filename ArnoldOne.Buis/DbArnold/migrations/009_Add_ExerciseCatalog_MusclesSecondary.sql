-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- Primära och sekundära muskler lagras separat (muskelfiguren). Kör sedan en synk i admin
-- så att kolumnerna fylls: Muscles blir bara primära, MusclesSecondary de sekundära.
IF COL_LENGTH(N'dbo.ExerciseCatalog', N'MusclesSecondary') IS NULL
    ALTER TABLE dbo.ExerciseCatalog ADD MusclesSecondary NVARCHAR (400) NOT NULL
        CONSTRAINT DF_ExerciseCatalog_MusclesSecondary DEFAULT (N'');
