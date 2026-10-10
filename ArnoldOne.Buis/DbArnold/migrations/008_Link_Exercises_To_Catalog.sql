-- Kör EFTER första synken i admin (Övningskatalog > Synka från wger), så att katalogen är fylld.
-- Kopplar mallövningar och användarnas övningar till katalogen där namnet matchar exakt
-- (skiftlägesokänsligt) mot svenskt namn, engelskt namn eller ett wger-alias.
-- Bara rader utan koppling ändras, och bara när namnet ger exakt en katalogträff. Skriptet går att köra om.
-- Kräver STRING_SPLIT (SQL Server 2016+ / Azure SQL, kompatibilitetsnivå 130+).
SET XACT_ABORT ON;
BEGIN TRANSACTION;

;WITH Names AS
(
    SELECT CatalogExerciseId, LOWER(LTRIM(RTRIM(NameSv))) AS Name FROM dbo.ExerciseCatalog WHERE IsActive = 1 AND NameSv IS NOT NULL
    UNION
    SELECT CatalogExerciseId, LOWER(LTRIM(RTRIM(NameEn))) FROM dbo.ExerciseCatalog WHERE IsActive = 1
    UNION
    SELECT c.CatalogExerciseId, LOWER(LTRIM(RTRIM(a.value)))
    FROM dbo.ExerciseCatalog c
    CROSS APPLY STRING_SPLIT(c.Aliases, CHAR(10)) a
    WHERE c.IsActive = 1 AND LTRIM(RTRIM(a.value)) <> N''
),
UniqueNames AS
(
    SELECT Name, MIN(CatalogExerciseId) AS CatalogExerciseId
    FROM Names
    GROUP BY Name
    HAVING COUNT(DISTINCT CatalogExerciseId) = 1
)
UPDATE t
SET CatalogExerciseId = u.CatalogExerciseId
FROM dbo.ExerciseTemplate t
JOIN UniqueNames u ON u.Name = LOWER(LTRIM(RTRIM(t.ExerciseTemplateName)))
WHERE t.CatalogExerciseId IS NULL;

PRINT CONCAT(N'Mallövningar kopplade: ', @@ROWCOUNT);

;WITH Names AS
(
    SELECT CatalogExerciseId, LOWER(LTRIM(RTRIM(NameSv))) AS Name FROM dbo.ExerciseCatalog WHERE IsActive = 1 AND NameSv IS NOT NULL
    UNION
    SELECT CatalogExerciseId, LOWER(LTRIM(RTRIM(NameEn))) FROM dbo.ExerciseCatalog WHERE IsActive = 1
    UNION
    SELECT c.CatalogExerciseId, LOWER(LTRIM(RTRIM(a.value)))
    FROM dbo.ExerciseCatalog c
    CROSS APPLY STRING_SPLIT(c.Aliases, CHAR(10)) a
    WHERE c.IsActive = 1 AND LTRIM(RTRIM(a.value)) <> N''
),
UniqueNames AS
(
    SELECT Name, MIN(CatalogExerciseId) AS CatalogExerciseId
    FROM Names
    GROUP BY Name
    HAVING COUNT(DISTINCT CatalogExerciseId) = 1
)
UPDATE e
SET CatalogExerciseId = u.CatalogExerciseId
FROM dbo.Exercise e
JOIN UniqueNames u ON u.Name = LOWER(LTRIM(RTRIM(e.ExerciseName)))
WHERE e.CatalogExerciseId IS NULL;

PRINT CONCAT(N'Användarövningar kopplade: ', @@ROWCOUNT);

-- Kvar utan koppling, för manuell genomgång (vanligaste namnen först).
SELECT ExerciseTemplateName AS Namn, COUNT(*) AS Antal, N'Mall' AS Typ
FROM dbo.ExerciseTemplate WHERE CatalogExerciseId IS NULL AND Visability = 1
GROUP BY ExerciseTemplateName
UNION ALL
SELECT ExerciseName, COUNT(*), N'Användare'
FROM dbo.Exercise WHERE CatalogExerciseId IS NULL AND Visability = 1
GROUP BY ExerciseName
ORDER BY Antal DESC;

COMMIT TRANSACTION;
