-- Kör i Arnold-databasen på localhost
CREATE TABLE UserSettings (
    UserId       UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    IsDarkMode   BIT              NOT NULL DEFAULT 0,
    IsRestTimerEnabled BIT        NOT NULL DEFAULT 1,
    RestTimerSeconds   INT        NOT NULL DEFAULT 120,
    CONSTRAINT FK_UserSettings_UserInfo FOREIGN KEY (UserId) REFERENCES UserInfo(UserId)
);
