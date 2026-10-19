-- Kör i databasen med ett konto som har ALTER-behörighet.
-- Valfri färg per pass (#RRGGBB ur PassColors), visas i aktivitetsrutnätet på översikten.
IF COL_LENGTH(N'dbo.Pass', N'Color') IS NULL
    ALTER TABLE dbo.Pass ADD Color VARCHAR (7) NULL;
