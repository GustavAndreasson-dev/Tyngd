CREATE TABLE [dbo].[SplitTemplate] (
    [SplitTemplateId]   INT          IDENTITY (1, 1) NOT NULL,
    [SplitTemplateName] VARCHAR (50) NOT NULL,
    [Visability]        BIT          NOT NULL,
    CONSTRAINT [PK_SplitTemplate] PRIMARY KEY CLUSTERED ([SplitTemplateId] ASC)
);

