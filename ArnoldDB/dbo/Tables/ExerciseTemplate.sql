CREATE TABLE [dbo].[ExerciseTemplate] (
    [ExerciseTemplateId]   INT          IDENTITY (1, 1) NOT NULL,
    [ExerciseTemplateName] VARCHAR (50) NOT NULL,
    [PassTemplateId]       INT          NOT NULL,
    [DefaultSets]          INT          NULL,
    [DefaultRepsMin]       INT          NULL,
    [DefaultRepsMax]       INT          NULL,
    [DefaultWeight]        INT          NULL,
    [Visability]           BIT          NOT NULL,
    [Ordning]              INT          CONSTRAINT [DF_ExerciseTemplate_Ordning] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_ExerciseTemplate] PRIMARY KEY CLUSTERED ([ExerciseTemplateId] ASC),
    CONSTRAINT [FK_ExerciseTemplate_PassTemplate] FOREIGN KEY ([PassTemplateId]) REFERENCES [dbo].[PassTemplate] ([PassTemplateId])
);

