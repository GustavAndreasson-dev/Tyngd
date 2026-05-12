CREATE TABLE [dbo].[SetLog] (
    [SetLogIId]     INT NOT NULL,
    [ExerciseLogID] INT NOT NULL,
    [SetIndex]      INT NOT NULL,
    [Reps]          INT NOT NULL,
    [IsDone]        BIT NOT NULL,
    CONSTRAINT [PK_SetLog] PRIMARY KEY CLUSTERED ([SetLogIId] ASC),
    CONSTRAINT [FK_SetLog_ExerciseLogId] FOREIGN KEY ([ExerciseLogID]) REFERENCES [dbo].[ExerciseLog] ([ExerciseLogId])
);

