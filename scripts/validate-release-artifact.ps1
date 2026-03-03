param(
    [string]$PublishDir = "./release/publish"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$target = Resolve-Path $PublishDir -ErrorAction SilentlyContinue
if (-not $target) {
    throw "Publish directory not found: $PublishDir"
}

$targetPath = $target.Path

$requiredFiles = @(
    "DoctorSoft.App.exe",
    "appsettings.json"
)

foreach ($required in $requiredFiles) {
    $full = Join-Path $targetPath $required
    if (-not (Test-Path $full)) {
        throw "Required file missing: $required"
    }
}

$appSettingsPath = Join-Path $targetPath "appsettings.json"
$appSettings = Get-Content $appSettingsPath -Raw | ConvertFrom-Json

if (-not $appSettings.Database.MainDbPath) {
    throw "Database.MainDbPath missing in appsettings.json"
}
if (-not $appSettings.App.LogDirectory) {
    throw "App.LogDirectory missing in appsettings.json"
}
if (-not $appSettings.App.BackupDirectory) {
    throw "App.BackupDirectory missing in appsettings.json"
}
if (-not $appSettings.App.MaintenanceHistoryFileScanLimit) {
    throw "App.MaintenanceHistoryFileScanLimit missing in appsettings.json"
}
if (-not $appSettings.App.MaintenanceHistoryDefaultMaxRows) {
    throw "App.MaintenanceHistoryDefaultMaxRows missing in appsettings.json"
}

Write-Host "Release artifact validation passed: $targetPath" -ForegroundColor Green
