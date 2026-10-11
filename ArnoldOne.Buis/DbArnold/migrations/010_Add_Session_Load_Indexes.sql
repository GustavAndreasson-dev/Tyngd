-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- Index för att öppna ett pass: senaste sessionen per pass, set per övningslogg och viktutveckling per övning.
-- Utan dem läses hela Session och SetLog vid varje passöppning (mätt: 30 000 sidläsningar -> under 40).
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SetLog_ExerciseLogID' AND object_id = OBJECT_ID(N'dbo.SetLog'))
    CREATE NONCLUSTERED INDEX IX_SetLog_ExerciseLogID
        ON dbo.SetLog (ExerciseLogID) INCLUDE (SetIndex, Reps, IsDone, Weight);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Session_PassId_StartedAtUtc' AND object_id = OBJECT_ID(N'dbo.Session'))
    CREATE NONCLUSTERED INDEX IX_Session_PassId_StartedAtUtc
        ON dbo.Session (PassId, StartedAtUtc DESC) INCLUDE (CompletedAtUtc, [Date], SessionKey);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ExerciseLog_ExerciseId' AND object_id = OBJECT_ID(N'dbo.ExerciseLog'))
    CREATE NONCLUSTERED INDEX IX_ExerciseLog_ExerciseId
        ON dbo.ExerciseLog (ExerciseId) INCLUDE (SessionId);

COMMIT TRANSACTION;
