CREATE TABLE [dbo].[Session] (
    [SessionId] INT  IDENTITY (1, 1) NOT NULL,
    [PassId]    INT  NOT NULL,
    [Date]      DATE NOT NULL,
    CONSTRAINT [PK_Session] PRIMARY KEY CLUSTERED ([SessionId] ASC),
    CONSTRAINT [FK_Session_Pass] FOREIGN KEY ([PassId]) REFERENCES [dbo].[Pass] ([PassId])
);

