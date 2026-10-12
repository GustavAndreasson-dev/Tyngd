-- Körda migreringsskript (Tyngd.Data/Migrations). Fylls av tools/run-migrations.ps1.
CREATE TABLE [dbo].[MigrationHistory] (
    [ScriptName]   NVARCHAR (200) NOT NULL,
    [Checksum]     CHAR (64)      NOT NULL,
    [AppliedAtUtc] DATETIMEOFFSET CONSTRAINT [DF_MigrationHistory_AppliedAtUtc] DEFAULT (SYSUTCDATETIME()) NOT NULL,
    [AppliedBy]    NVARCHAR (128) CONSTRAINT [DF_MigrationHistory_AppliedBy] DEFAULT (SUSER_SNAME()) NOT NULL,
    [Baselined]    BIT            CONSTRAINT [DF_MigrationHistory_Baselined] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_MigrationHistory] PRIMARY KEY CLUSTERED ([ScriptName] ASC)
);
