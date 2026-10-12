<#
.SYNOPSIS
    Creates an empty local database from the ArnoldDB schema scripts and marks all migrations as applied.

.DESCRIPTION
    Defaults to LocalDB: server (localdb)\MSSQLLocalDB, database ArnoldOne, Windows authentication.
    Override with ARNOLD_SQL_SERVER / ARNOLD_SQL_DATABASE. Local servers only: the script refuses any
    server name that is not localhost, '.', or a LocalDB instance.

.EXAMPLE
    ./tools/create-local-db.ps1
    ./tools/create-local-db.ps1 -Recreate    # drops and recreates the database first
#>
param([switch] $Recreate)

$ErrorActionPreference = 'Stop'

$server   = if ($env:ARNOLD_SQL_SERVER) { $env:ARNOLD_SQL_SERVER } else { '(localdb)\MSSQLLocalDB' }
$database = if ($env:ARNOLD_SQL_DATABASE) { $env:ARNOLD_SQL_DATABASE } else { 'ArnoldOne' }

if ($server -notmatch '^(\(localdb\)|localhost|\.|127\.0\.0\.1)([\x5C].*)?$') {
    throw "Refusing to create a database on '$server'. Only local servers are allowed."
}

$schemaDir = Join-Path $PSScriptRoot '..\ArnoldDB\dbo' | Resolve-Path
# Dependency order: referenced tables first.
$tables = 'UserInfo', 'UserSettings', 'SplitTemplate', 'PassTemplate', 'ExerciseCatalog', 'ExerciseTemplate',
          'Split', 'Pass', 'Exercise', 'Session', 'ExerciseLog', 'SetLog', 'UserFavoriteExercise'

function Invoke-Sqlcmd2([string[]] $Arguments) {
    & sqlcmd -S $server -E -C -b -I @Arguments
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed ($($Arguments -join ' '))" }
}

if ($Recreate) {
    Invoke-Sqlcmd2 '-Q', "IF DB_ID('$database') IS NOT NULL BEGIN ALTER DATABASE [$database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$database]; END"
}
elseif (& sqlcmd -S $server -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT DB_ID('$database')" | Where-Object { $_ -match '^\d+$' }) {
    throw "Database '$database' already exists. Use -Recreate to drop it, or ./tools/run-migrations.ps1 to update it."
}

Invoke-Sqlcmd2 '-Q', "CREATE DATABASE [$database] COLLATE Latin1_General_CI_AS"
foreach ($table in $tables) { Invoke-Sqlcmd2 '-d', $database, '-i', (Join-Path $schemaDir "Tables\$table.sql") }
foreach ($index in Get-ChildItem (Join-Path $schemaDir 'Indexes') -Filter '*.sql') { Invoke-Sqlcmd2 '-d', $database, '-i', $index.FullName }

# The schema scripts already contain every migration; record them so run-migrations only runs future ones.
$env:ARNOLD_SQL_SERVER = $server
$env:ARNOLD_SQL_DATABASE = $database
& (Join-Path $PSScriptRoot 'run-migrations.ps1') -Target local -Baseline 999
Write-Host "Database '$database' is ready on $server."
