-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.key_constraints
    WHERE [type] = 'PK'
      AND parent_object_id = OBJECT_ID(N'dbo.ExerciseTemplate')
)
BEGIN
    ALTER TABLE dbo.ExerciseTemplate
        ADD CONSTRAINT PK_ExerciseTemplate
        PRIMARY KEY CLUSTERED (ExerciseTemplateId);
END;

COMMIT TRANSACTION;
