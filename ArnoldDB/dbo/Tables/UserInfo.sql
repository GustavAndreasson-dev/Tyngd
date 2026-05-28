CREATE TABLE [dbo].[UserInfo] (
    [UserId]       UNIQUEIDENTIFIER NOT NULL,
    [UserName]     VARCHAR (50)     NOT NULL,
    [Email]        VARCHAR (50)     NOT NULL,
    [CreatedAt]    DATETIME         NOT NULL,
    [Roles]        VARCHAR (500)    NOT NULL,
    [PasswordHash] VARCHAR (256)    DEFAULT ('') NOT NULL,
    CONSTRAINT [PK_UserInfo] PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [UQ_UserInfo_UserName] UNIQUE ([UserName]),
    CONSTRAINT [UQ_UserInfo_Email] UNIQUE ([Email])
);

