CREATE TABLE [dbo].[Session] (
    [SessionId] INT  IDENTITY (1, 1) NOT NULL,
    [PassId]    INT  NOT NULL,
    [Date]      DATE NOT NULL,
    [SessionKey] UNIQUEIDENTIFIER CONSTRAINT [DF_Session_SessionKey] DEFAULT (NEWID()) NOT NULL,
    [StartedAtUtc] DATETIMEOFFSET CONSTRAINT [DF_Session_StartedAtUtc] DEFAULT (SYSUTCDATETIME()) NOT NULL,
    [CompletedAtUtc] DATETIMEOFFSET NULL,
    CONSTRAINT [PK_Session] PRIMARY KEY CLUSTERED ([SessionId] ASC),
    CONSTRAINT [FK_Session_Pass] FOREIGN KEY ([PassId]) REFERENCES [dbo].[Pass] ([PassId])
);

GO

CREATE UNIQUE INDEX [UX_Session_SessionKey]
    ON [dbo].[Session]([SessionKey]);

