param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputRoot = "./release/publish"
)

$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path -Parent $PSScriptRoot
Set-Location $solutionRoot

$appProject = Join-Path $solutionRoot "DoctorSoft.App/DoctorSoft.App.csproj"
if (-not (Test-Path $appProject)) {
    throw "App project not found at $appProject"
}

$publishOutput = Join-Path $solutionRoot $OutputRoot
if (-not (Test-Path $publishOutput)) {
    New-Item -ItemType Directory -Path $publishOutput | Out-Null
}

Write-Host "Publishing DoctorSoft.App..." -ForegroundColor Cyan

dotnet publish $appProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained false `
    /p:PublishSingleFile=false `
    -o $publishOutput

$appSettingsPath = Join-Path $publishOutput "appsettings.json"
if (Test-Path $appSettingsPath) {
    $appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json

    if (-not $appSettings.App) {
        $appSettings | Add-Member -MemberType NoteProperty -Name App -Value (@{})
    }

    if (-not $appSettings.App.LogDirectory) { $appSettings.App.LogDirectory = "logs" }
    if (-not $appSettings.App.BackupDirectory) { $appSettings.App.BackupDirectory = "backups" }
    if (-not $appSettings.App.MaintenanceHistoryFileScanLimit) { $appSettings.App.MaintenanceHistoryFileScanLimit = 15 }
    if (-not $appSettings.App.MaintenanceHistoryDefaultMaxRows) { $appSettings.App.MaintenanceHistoryDefaultMaxRows = 500 }

    $appSettings | ConvertTo-Json -Depth 8 | Set-Content $appSettingsPath -Encoding UTF8
}

$releaseReadme = Join-Path $publishOutput "RELEASE_NOTES.txt"
@(
    "DoctorSoft C# Release Publish"
    "Configuration: $Configuration"
    "Runtime: $Runtime"
    "Published: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    ""
    "Post-publish checks:"
    "1. Verify appsettings.json Database.MainDbPath points to production DB location."
    "2. Verify App.LogDirectory and App.BackupDirectory paths are writable."
    "3. Launch DoctorSoft.App.exe and confirm login + module shell opens."
) | Set-Content $releaseReadme -Encoding UTF8

Write-Host "Publish completed: $publishOutput" -ForegroundColor Green
