CREATE TABLE [dbo].[Exercise] (
    [ExerciseId]     INT             IDENTITY (1, 1) NOT NULL,
    [PassId]         INT             NOT NULL,
    [ExerciseName]   VARCHAR (50)    NOT NULL,
    [DefaultSets]    INT             NULL,
    [DefautlRepsMin] INT             NULL,
    [DefaultRepsMax] INT             NULL,
    [DefaultWeight]  DECIMAL (10, 2) NULL,
    [Ordning]        INT             CONSTRAINT [DF_Exercise_Ordning] DEFAULT ((1)) NOT NULL,
    [Visability]     BIT             CONSTRAINT [DF_Exercise_Visability] DEFAULT ((1)) NOT NULL,
    [CatalogExerciseId] INT          NULL,
    CONSTRAINT [PK_Exercise] PRIMARY KEY CLUSTERED ([ExerciseId] ASC),
    CONSTRAINT [FK_Exercise_Pass] FOREIGN KEY ([PassId]) REFERENCES [dbo].[Pass] ([PassId]),
    CONSTRAINT [FK_Exercise_ExerciseCatalog] FOREIGN KEY ([CatalogExerciseId]) REFERENCES [dbo].[ExerciseCatalog] ([CatalogExerciseId])
);


GO

CREATE NONCLUSTERED INDEX [IX_Exercise_CatalogExerciseId]
    ON [dbo].[Exercise]([CatalogExerciseId]) INCLUDE ([PassId])
    WHERE [CatalogExerciseId] IS NOT NULL;
