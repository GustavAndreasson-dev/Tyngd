CREATE TABLE [dbo].[Exercise] (
    [ExerciseId]     INT             IDENTITY (1, 1) NOT NULL,
    [PassId]         INT             NOT NULL,
    [ExerciseName]   VARCHAR (50)    NOT NULL,
    [DefaultSets]    INT             NULL,
    [DefautlRepsMin] INT             NULL,
    [DefaultRepsMax] INT             NULL,
    [DefaultWeight]  DECIMAL (10, 2) NULL,
    CONSTRAINT [PK_Exercise] PRIMARY KEY CLUSTERED ([ExerciseId] ASC),
    CONSTRAINT [FK_Exercise_Pass] FOREIGN KEY ([PassId]) REFERENCES [dbo].[Pass] ([PassId])
);

