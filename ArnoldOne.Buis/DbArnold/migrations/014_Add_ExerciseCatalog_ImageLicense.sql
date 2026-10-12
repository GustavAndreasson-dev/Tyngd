-- Kör i databasen med ett konto som har ALTER-behörighet.
-- Bilderna från wger har egen upphovsperson och licens som ska visas tillsammans med bilden.
-- Kör sedan en synk i admin så att kolumnerna fylls.
IF COL_LENGTH(N'dbo.ExerciseCatalog', N'ImageAuthor') IS NULL
    ALTER TABLE dbo.ExerciseCatalog ADD ImageAuthor NVARCHAR (200) NULL;

IF COL_LENGTH(N'dbo.ExerciseCatalog', N'ImageLicense') IS NULL
    ALTER TABLE dbo.ExerciseCatalog ADD ImageLicense NVARCHAR (50) NULL;
