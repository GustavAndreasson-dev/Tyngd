<#
.SYNOPSIS
    Kör databasmigreringar (ArnoldOne.Buis/DbArnold/migrations/NNN_*.sql) som inte körts, i nummerordning,
    och loggar varje körning i dbo.MigrationHistory.

.DESCRIPTION
    Bara mål i listan nedan accepteras, så produktion kan inte träffas av misstag.
    Varje skript hanterar sin egen transaktion. Körningen stoppar vid första fel; skriptet som felade loggas inte.

.EXAMPLE
    ./tools/run-migrations.ps1 -Target local -List
    Visar vilka skript som är körda och vilka som väntar.

.EXAMPLE
    ./tools/run-migrations.ps1 -Target local -Baseline 9
    Registrerar 001-009 som redan körda (utan att köra dem). Används en gång för en databas
    där skripten körts för hand innan historiken fanns.

.EXAMPLE
    ./tools/run-migrations.ps1 -Target local
    Kör alla väntande skript.
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('local')]
    [string] $Target,

    [switch] $List,

    [int] $Baseline = 0
)

$ErrorActionPreference = 'Stop'

# Tillåtna mål. Lägg till test-dev här när inloggning finns; prod ska aldrig stå med.
$targets = @{
    local = @{ Server = 'localhost'; Database = 'Arnold'; Auth = @('-E') }
}
$config = $targets[$Target]
$migrationsDir = Join-Path $PSScriptRoot '..\ArnoldOne.Buis\DbArnold\migrations' | Resolve-Path

function Invoke-Sql([string] $Query) {
    $output = & sqlcmd -S $config.Server -d $config.Database @($config.Auth) -C -b -I -W -h -1 -Q "SET NOCOUNT ON; $Query"
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd misslyckades: $output" }
    return $output
}

# Historiktabellen skapas vid första körningen (samma definition som i ArnoldDB-projektet).
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

Write-Host "Mål: $Target ($($config.Server) / $($config.Database))"
$pending = @()
foreach ($script in $scripts) {
    $number = [int]$script.Name.Substring(0, 3)
    if ($applied.ContainsKey($script.Name)) {
        $note = if ($applied[$script.Name] -ne (Get-Checksum $script)) { '  (filen har ändrats sedan körningen)' } else { '' }
        Write-Host "  körd     $($script.Name)$note"
    }
    elseif ($Baseline -ge $number) {
        $pending += [pscustomobject]@{ Script = $script; Baseline = $true }
        Write-Host "  baseline $($script.Name)"
    }
    else {
        $pending += [pscustomobject]@{ Script = $script; Baseline = $false }
        Write-Host "  väntar   $($script.Name)"
    }
}

if ($List -or $pending.Count -eq 0) {
    if ($pending.Count -eq 0) { Write-Host 'Inget att köra.' }
    return
}

foreach ($item in $pending) {
    $script = $item.Script
    $name = $script.Name.Replace("'", "''")
    $sum = Get-Checksum $script
    if (-not $item.Baseline) {
        Write-Host "Kör $($script.Name) ..."
        & sqlcmd -S $config.Server -d $config.Database @($config.Auth) -C -b -I -i $script.FullName
        if ($LASTEXITCODE -ne 0) { throw "$($script.Name) misslyckades. Senare skript kördes inte." }
    }
    Invoke-Sql "INSERT INTO dbo.MigrationHistory (ScriptName, Checksum, Baselined) VALUES (N'$name', '$sum', $([int]$item.Baseline));" | Out-Null
}
Write-Host 'Klart.'
