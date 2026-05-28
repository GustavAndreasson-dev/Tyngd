CREATE TABLE [dbo].[UserSettings] (
    [UserId]             UNIQUEIDENTIFIER NOT NULL,
    [IsDarkMode]         BIT              DEFAULT ((0)) NOT NULL,
    [IsRestTimerEnabled] BIT              DEFAULT ((1)) NOT NULL,
    [RestTimerSeconds]   INT              DEFAULT ((120)) NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserSettings_UserInfo] FOREIGN KEY ([UserId]) REFERENCES [dbo].[UserInfo] ([UserId])
);

