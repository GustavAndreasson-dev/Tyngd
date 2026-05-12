CREATE TABLE [dbo].[UserInfo] (
    [UserId]    UNIQUEIDENTIFIER NOT NULL,
    [UserName]  VARCHAR (50)     NOT NULL,
    [Email]     VARCHAR (50)     NOT NULL,
    [CreatedAt] DATETIME         NOT NULL,
    [Roles]     VARCHAR (500)    NOT NULL,
    CONSTRAINT [PK_UserInfo] PRIMARY KEY CLUSTERED ([UserId] ASC)
);

