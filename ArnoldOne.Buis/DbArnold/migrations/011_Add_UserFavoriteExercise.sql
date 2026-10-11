-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- Användarens favoritövningar ur katalogen.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.UserFavoriteExercise', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserFavoriteExercise
    (
        UserId            UNIQUEIDENTIFIER NOT NULL,
        CatalogExerciseId INT              NOT NULL,
        CreatedAtUtc      DATETIMEOFFSET   NOT NULL CONSTRAINT DF_UserFavoriteExercise_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserFavoriteExercise PRIMARY KEY CLUSTERED (UserId, CatalogExerciseId),
        CONSTRAINT FK_UserFavoriteExercise_UserInfo FOREIGN KEY (UserId) REFERENCES dbo.UserInfo (UserId),
        CONSTRAINT FK_UserFavoriteExercise_ExerciseCatalog FOREIGN KEY (CatalogExerciseId) REFERENCES dbo.ExerciseCatalog (CatalogExerciseId)
    );
END;

COMMIT TRANSACTION;
