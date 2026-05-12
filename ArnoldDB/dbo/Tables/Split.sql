CREATE TABLE [dbo].[Split] (
    [SplitId]   INT              IDENTITY (1, 1) NOT NULL,
    [SplitName] VARCHAR (50)     NOT NULL,
    [UserId]    UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT [PK_Split] PRIMARY KEY CLUSTERED ([SplitId] ASC),
    CONSTRAINT [FK_Split_UserInfo] FOREIGN KEY ([UserId]) REFERENCES [dbo].[UserInfo] ([UserId])
);

