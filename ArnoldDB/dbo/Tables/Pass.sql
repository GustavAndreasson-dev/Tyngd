CREATE TABLE [dbo].[Pass] (
    [PassId]         INT          IDENTITY (1, 1) NOT NULL,
    [SplitId]        INT          NOT NULL,
    [PassName]       VARCHAR (50) NOT NULL,
    [Visability]     INT          NOT NULL,
    [Ordning]        INT          CONSTRAINT [DF_Pass_Ordning] DEFAULT ((1)) NOT NULL,
    [PassTemplateId] INT          NULL,
    CONSTRAINT [PK_Pass] PRIMARY KEY CLUSTERED ([PassId] ASC),
    CONSTRAINT [FK_Pass_Split] FOREIGN KEY ([SplitId]) REFERENCES [dbo].[Split] ([SplitId])
);

