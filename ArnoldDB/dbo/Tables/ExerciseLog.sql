CREATE TABLE [dbo].[ExerciseLog] (
    [ExerciseLogId] INT            IDENTITY (1, 1) NOT NULL,
    [SessionId]     INT            NOT NULL,
    [ExerciseId]    INT            NOT NULL,
    [NoteText]      VARCHAR (5000) NULL,
    CONSTRAINT [PK_ExerciseLogId] PRIMARY KEY CLUSTERED ([ExerciseLogId] ASC),
    CONSTRAINT [FK_ExerciseLogId_Exercise] FOREIGN KEY ([ExerciseId]) REFERENCES [dbo].[Exercise] ([ExerciseId]),
    CONSTRAINT [FK_ExerciseLogId_Session] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[Session] ([SessionId])
);

