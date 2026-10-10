-- Kör i Arnold-databasen med ett konto som har ALTER-behörighet.
-- Övningskatalog importerad från wger.de, och valfri koppling från övningar och mallövningar.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ExerciseCatalog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExerciseCatalog
    (
        CatalogExerciseId INT              IDENTITY (1, 1) NOT NULL,
        WgerUuid          UNIQUEIDENTIFIER NOT NULL,
        NameEn            NVARCHAR (100)   NOT NULL,
        DescriptionEn     NVARCHAR (MAX)   NOT NULL,
        NameSv            NVARCHAR (100)   NULL,
        DescriptionSv     NVARCHAR (MAX)   NULL,
        TranslationStatus TINYINT          NOT NULL CONSTRAINT DF_ExerciseCatalog_TranslationStatus DEFAULT ((0)),
        SourceHash        CHAR (64)        NOT NULL,
        Category          NVARCHAR (50)    NOT NULL,
        Muscles           NVARCHAR (400)   NOT NULL,
        Equipment         NVARCHAR (400)   NOT NULL,
        Aliases           NVARCHAR (1000)  NOT NULL,
        ImageUrl          NVARCHAR (400)   NULL,
        License           NVARCHAR (100)   NOT NULL,
        LicenseAuthor     NVARCHAR (400)   NOT NULL,
        IsActive          BIT              NOT NULL CONSTRAINT DF_ExerciseCatalog_IsActive DEFAULT ((1)),
        LastSyncedUtc     DATETIMEOFFSET   NOT NULL,
        CONSTRAINT PK_ExerciseCatalog PRIMARY KEY CLUSTERED (CatalogExerciseId ASC),
        CONSTRAINT UQ_ExerciseCatalog_WgerUuid UNIQUE (WgerUuid)
    );
END;

IF COL_LENGTH(N'dbo.Exercise', N'CatalogExerciseId') IS NULL
    ALTER TABLE dbo.Exercise ADD CatalogExerciseId INT NULL
        CONSTRAINT FK_Exercise_ExerciseCatalog REFERENCES dbo.ExerciseCatalog (CatalogExerciseId);

IF COL_LENGTH(N'dbo.ExerciseTemplate', N'CatalogExerciseId') IS NULL
    ALTER TABLE dbo.ExerciseTemplate ADD CatalogExerciseId INT NULL
        CONSTRAINT FK_ExerciseTemplate_ExerciseCatalog REFERENCES dbo.ExerciseCatalog (CatalogExerciseId);

COMMIT TRANSACTION;
