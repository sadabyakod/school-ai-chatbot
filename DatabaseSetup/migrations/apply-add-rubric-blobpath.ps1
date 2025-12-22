<#
.SYNOPSIS
  Apply the add-rubric-blobpath migration to the target database.

.DESCRIPTION
  This script runs the SQL file DatabaseSetup/migrations/add-rubric-blobpath-to-subjective-rubrics.sql
  against the target database. It prefers using the SqlServer PowerShell module (Invoke-Sqlcmd).
  If that module is not available, it falls back to using the `sqlcmd` CLI if present.

.PARAMETER SqlFilePath
  Path to the .sql file to execute. Defaults to the migration file in this folder.

.PARAMETER ConnectionString
  Full ADO.NET connection string (preferred). If omitted, provide ServerInstance + Database (+ creds).

.PARAMETER ServerInstance
  SQL Server host/instance (e.g. tcp:yourserver.database.windows.net,1433)

.PARAMETER Database
  Database name

.PARAMETER Username
  SQL auth username (optional if using integrated auth)

.PARAMETER Password
  SQL auth password

.PARAMETER Force
  If supplied, do not prompt for confirmation.

.EXAMPLE
  # Using a connection string stored in env
  $env:DB_CONN = "Server=tcp:myserver.database.windows.net,1433;Initial Catalog=MyDb;User ID=sqluser;Password=Secret;Encrypt=True"
  .\apply-add-rubric-blobpath.ps1 -ConnectionString $env:DB_CONN -Force

.EXAMPLE
  .\apply-add-rubric-blobpath.ps1 -ServerInstance "tcp:myserver.database.windows.net,1433" -Database "MyDb" -Username "sqluser" -Password "Secret"

#>
param(
    [string]$SqlFilePath = "DatabaseSetup/migrations/add-rubric-blobpath-to-subjective-rubrics.sql",
    [string]$ConnectionString,
    [string]$ServerInstance,
    [string]$Database,
    [string]$Username,
    [string]$Password,
    [switch]$Force
)

# Resolve full path
$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$SqlFileFullPath = Join-Path $ScriptRoot $SqlFilePath
if (-not (Test-Path $SqlFileFullPath)) {
    Write-Error "SQL file not found: $SqlFileFullPath"
    exit 2
}

Write-Host "Migration SQL: $SqlFileFullPath" -ForegroundColor Cyan

if (-not $Force) {
    $confirm = Read-Host "About to run migration. Type YES to continue"
    if ($confirm -ne 'YES') {
        Write-Host "Aborted by user." -ForegroundColor Yellow
        exit 0
    }
}

# Try to use Invoke-Sqlcmd from SqlServer module
$used = $false
try {
    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Write-Host "SqlServer module not found. Attempting to install for current user..." -ForegroundColor Yellow
        try {
            Install-Module SqlServer -Scope CurrentUser -Force -AllowClobber -ErrorAction Stop
        }
        catch {
            Write-Warning "Failed to install SqlServer module: $_. Exception. Will attempt sqlcmd fallback."
        }
    }

    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer -ErrorAction Stop

        if ($ConnectionString) {
            Write-Host "Running Invoke-Sqlcmd with provided ConnectionString..." -ForegroundColor Green
            Invoke-Sqlcmd -ConnectionString $ConnectionString -InputFile $SqlFileFullPath -QueryTimeout 0
            $used = $true
        }
        else {
            if (-not $ServerInstance -or -not $Database) {
                Write-Warning "ServerInstance and Database must be provided when not using ConnectionString. Falling back to sqlcmd if available."
            }
            else {
                if ($Username -and $Password) {
                    Write-Host "Running Invoke-Sqlcmd with SQL authentication..." -ForegroundColor Green
                    $securePwd = ConvertTo-SecureString $Password -AsPlainText -Force
                    $cred = New-Object System.Management.Automation.PSCredential($Username, $securePwd)
                    Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -Username $Username -Password $Password -InputFile $SqlFileFullPath -QueryTimeout 0
                    $used = $true
                }
                else {
                    Write-Host "Running Invoke-Sqlcmd using Windows Integrated Authentication..." -ForegroundColor Green
                    Invoke-Sqlcmd -ServerInstance $ServerInstance -Database $Database -InputFile $SqlFileFullPath -QueryTimeout 0
                    $used = $true
                }
            }
        }
    }
}
catch {
    Write-Warning "Invoke-Sqlcmd attempt failed: $_"
    $used = $false
}

if (-not $used) {
    Write-Host "Attempting fallback via sqlcmd.exe" -ForegroundColor Yellow

    $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        Write-Error "sqlcmd.exe not found and Invoke-Sqlcmd unavailable. Install the SqlServer module or sqlcmd CLI (part of SQL Server tools)."
        exit 3
    }

    $sqlcmdArgs = "-i `"$SqlFileFullPath`""

    if ($ConnectionString) {
        # Try parse connection string for server and database as best-effort
        $cs = $ConnectionString
        $server = ($cs -split ';' | Where-Object { $_ -match '^Server=|^Data Source=|^Server=' } | Select-Object -First 1)
        $database = ($cs -split ';' | Where-Object { $_ -match '^Initial Catalog=|^Database=' } | Select-Object -First 1)
        if ($server) { $serverVal = ($server -split '=')[1]; $sqlcmdArgs = "-S `"$serverVal`" $sqlcmdArgs" }
        if ($database) { $dbVal = ($database -split '=')[1]; $sqlcmdArgs = "$sqlcmdArgs -d `"$dbVal`"" }
        # If username/password present, add -U -P
        $user = ($cs -split ';' | Where-Object { $_ -match '^User ID=' } | Select-Object -First 1)
        $pwd = ($cs -split ';' | Where-Object { $_ -match '^Password=' } | Select-Object -First 1)
        if ($user -and $pwd) { $userVal = ($user -split '=')[1]; $pwdVal = ($pwd -split '=')[1]; $sqlcmdArgs = "-U `"$userVal`" -P `"$pwdVal`" $sqlcmdArgs" }
    }
    elseif ($ServerInstance -and $Database) {
        $sqlcmdArgs = "-S `"$ServerInstance`" -d `"$Database`" $sqlcmdArgs"
        if ($Username -and $Password) { $sqlcmdArgs = "-U `"$Username`" -P `"$Password`" $sqlcmdArgs" }
    }
    else {
        Write-Error "Insufficient connection info for sqlcmd fallback. Provide -ConnectionString or -ServerInstance and -Database."
        exit 4
    }

    Write-Host "Executing: sqlcmd $sqlcmdArgs" -ForegroundColor Cyan
    $proc = Start-Process -FilePath sqlcmd -ArgumentList $sqlcmdArgs -NoNewWindow -Wait -PassThru
    if ($proc.ExitCode -ne 0) {
        Write-Error "sqlcmd returned exit code $($proc.ExitCode)"
        exit $proc.ExitCode
    }
}

Write-Host "Migration applied successfully (or no-op if column already existed)." -ForegroundColor Green
exit 0
