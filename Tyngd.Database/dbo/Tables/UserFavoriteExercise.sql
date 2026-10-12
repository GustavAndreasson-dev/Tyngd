CREATE TABLE [dbo].[UserFavoriteExercise] (
    [UserId]            UNIQUEIDENTIFIER NOT NULL,
    [CatalogExerciseId] INT              NOT NULL,
    [CreatedAtUtc]      DATETIMEOFFSET   CONSTRAINT [DF_UserFavoriteExercise_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()) NOT NULL,
    CONSTRAINT [PK_UserFavoriteExercise] PRIMARY KEY CLUSTERED ([UserId] ASC, [CatalogExerciseId] ASC),
    CONSTRAINT [FK_UserFavoriteExercise_UserInfo] FOREIGN KEY ([UserId]) REFERENCES [dbo].[UserInfo] ([UserId]),
    CONSTRAINT [FK_UserFavoriteExercise_ExerciseCatalog] FOREIGN KEY ([CatalogExerciseId]) REFERENCES [dbo].[ExerciseCatalog] ([CatalogExerciseId])
);
