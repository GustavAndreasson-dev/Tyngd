CREATE TABLE [dbo].[PassTemplate] (
    [PassTemplateId]   INT          IDENTITY (1, 1) NOT NULL,
    [SplitTemplateID]  INT          NOT NULL,
    [PassTemplateName] VARCHAR (50) NOT NULL,
    [Visability]       BIT          NOT NULL,
    [Ordning]          INT          CONSTRAINT [DF_PassTemplate_Ordning] DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_PassTemplate] PRIMARY KEY CLUSTERED ([PassTemplateId] ASC),
    CONSTRAINT [FK_PassTemplate_SplitTemplate] FOREIGN KEY ([SplitTemplateID]) REFERENCES [dbo].[SplitTemplate] ([SplitTemplateId])
);

