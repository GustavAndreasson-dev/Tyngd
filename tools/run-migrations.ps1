<#
.SYNOPSIS
    Runs database migrations (Tyngd.Data/Migrations/NNN_*.sql) that have not been run yet,
    in numeric order, and records each one in dbo.MigrationHistory.

.DESCRIPTION
    Only allow-listed targets are accepted, so production can never be hit by mistake.
    Each script handles its own transaction. The run stops at the first failure; the failing script is not recorded.

.EXAMPLE
    ./tools/run-migrations.ps1 -Target local -List
    Shows which scripts have run and which are pending.

.EXAMPLE
    ./tools/run-migrations.ps1 -Target local -Baseline 9
    Records scripts 001-009 as already applied without running them. Use once for a database
    where the scripts were run by hand before the history table existed.

.EXAMPLE
    ./tools/run-migrations.ps1 -Target local
    Runs all pending scripts.
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('local')]
    [string] $Target,

    [switch] $List,

    [int] $Baseline = 0
)

$ErrorActionPreference = 'Stop'

# Allowed targets only, so production can never be hit by mistake. 'local' uses Windows authentication
# against LocalDB by default; override with TYNGD_SQL_SERVER / TYNGD_SQL_DATABASE for another local server.
$serverOverride   = if ($env:TYNGD_SQL_SERVER) { $env:TYNGD_SQL_SERVER } else { $env:ARNOLD_SQL_SERVER }  # ARNOLD_*: names from before the rename
$databaseOverride = if ($env:TYNGD_SQL_DATABASE) { $env:TYNGD_SQL_DATABASE } else { $env:ARNOLD_SQL_DATABASE }
$targets = @{
    local = @{
        Server   = if ($serverOverride) { $serverOverride } else { '(localdb)\MSSQLLocalDB' }
        Database = if ($databaseOverride) { $databaseOverride } else { 'Tyngd' }
        Auth     = @('-E')
    }
}
$config = $targets[$Target]
$migrationsDir = Join-Path $PSScriptRoot '..\Tyngd.Data\Migrations' | Resolve-Path

function Invoke-Sql([string] $Query) {
    $output = & sqlcmd -S $config.Server -d $config.Database @($config.Auth) -C -b -I -W -h -1 -Q "SET NOCOUNT ON; $Query"
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed: $output" }
    return $output
}

# The history table is created on first use (same definition as in the Tyngd.Database project).
Invoke-Sql @"
IF OBJECT_ID(N'dbo.MigrationHistory', N'U') IS NULL
    CREATE TABLE dbo.MigrationHistory (
        ScriptName   NVARCHAR (200)  NOT NULL CONSTRAINT PK_MigrationHistory PRIMARY KEY,
        Checksum     CHAR (64)       NOT NULL,
        AppliedAtUtc DATETIMEOFFSET  NOT NULL CONSTRAINT DF_MigrationHistory_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
        AppliedBy    NVARCHAR (128)  NOT NULL CONSTRAINT DF_MigrationHistory_AppliedBy DEFAULT (SUSER_SNAME()),
        Baselined    BIT             NOT NULL CONSTRAINT DF_MigrationHistory_Baselined DEFAULT (0)
    );
"@ | Out-Null

$applied = @{}
foreach ($line in Invoke-Sql "SELECT ScriptName + '|' + Checksum FROM dbo.MigrationHistory") {
    if ($line) { $name, $sum = $line.Trim() -split '\|'; $applied[$name] = $sum }
}

$scripts = Get-ChildItem $migrationsDir -Filter '*.sql' |
    Where-Object { $_.Name -match '^(\d{3})_' } |
    Sort-Object { [int]($_.Name.Substring(0, 3)) }

function Get-Checksum($file) { (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() }

Write-Host "Target: $Target ($($config.Server) / $($config.Database))"
$pending = @()
foreach ($script in $scripts) {
    $number = [int]$script.Name.Substring(0, 3)
    if ($applied.ContainsKey($script.Name)) {
        $note = if ($applied[$script.Name] -ne (Get-Checksum $script)) { '  (file changed since it was run)' } else { '' }
        Write-Host "  applied   $($script.Name)$note"
    }
    elseif ($Baseline -ge $number) {
        $pending += [pscustomobject]@{ Script = $script; Baseline = $true }
        Write-Host "  baseline  $($script.Name)"
    }
    else {
        $pending += [pscustomobject]@{ Script = $script; Baseline = $false }
        Write-Host "  pending   $($script.Name)"
    }
}

if ($List -or $pending.Count -eq 0) {
    if ($pending.Count -eq 0) { Write-Host 'Nothing to run.' }
    return
}

foreach ($item in $pending) {
    $script = $item.Script
    $name = $script.Name.Replace("'", "''")
    $sum = Get-Checksum $script
    if (-not $item.Baseline) {
        Write-Host "Running $($script.Name) ..."
        & sqlcmd -S $config.Server -d $config.Database @($config.Auth) -C -b -I -i $script.FullName
        if ($LASTEXITCODE -ne 0) { throw "$($script.Name) failed. Later scripts were not run." }
    }
    Invoke-Sql "INSERT INTO dbo.MigrationHistory (ScriptName, Checksum, Baselined) VALUES (N'$name', '$sum', $([int]$item.Baseline));" | Out-Null
}
Write-Host 'Done.'
