CREATE TABLE [dbo].[Split] (
    [SplitId]         INT              IDENTITY (1, 1) NOT NULL,
    [SplitName]       VARCHAR (50)     NOT NULL,
    [UserId]          UNIQUEIDENTIFIER NOT NULL,
    [Visability]      BIT              CONSTRAINT [DF_Split_Visability] DEFAULT ((1)) NOT NULL,
    [SplitTemplateId] INT              NULL,
    CONSTRAINT [PK_Split] PRIMARY KEY CLUSTERED ([SplitId] ASC),
    CONSTRAINT [FK_Split_UserInfo] FOREIGN KEY ([UserId]) REFERENCES [dbo].[UserInfo] ([UserId])
);

