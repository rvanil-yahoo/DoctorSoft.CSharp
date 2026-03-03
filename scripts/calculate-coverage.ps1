param(
    [string]$TestProject = "./DoctorSoft.Tests/DoctorSoft.Tests.csproj",
    [string]$Configuration = "Debug",
    [string]$ResultsDirectory = "./DoctorSoft.Tests/TestResults/CoverageRuns",
    [double]$MinimumLineRate = 0.80,
    [switch]$SkipTestRun
)

$ErrorActionPreference = "Stop"

$solutionRoot = Split-Path -Parent $PSScriptRoot
Set-Location $solutionRoot

$testProjectPath = Resolve-Path $TestProject -ErrorAction SilentlyContinue
if (-not $testProjectPath) {
    throw "Test project not found: $TestProject"
}

$resultsPath = Join-Path $solutionRoot $ResultsDirectory
if (-not (Test-Path $resultsPath)) {
    New-Item -ItemType Directory -Path $resultsPath -Force | Out-Null
}

if (-not $SkipTestRun) {
    Write-Host "Running tests with coverage collection..." -ForegroundColor Cyan

    dotnet test $testProjectPath.Path `
        -c $Configuration `
        --collect:"XPlat Code Coverage" `
        --results-directory $resultsPath

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

$coverageFiles = Get-ChildItem -Path $resultsPath -Recurse -Filter "coverage.cobertura.xml" |
    Sort-Object LastWriteTimeUtc -Descending

if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
    throw "No coverage.cobertura.xml found under $resultsPath"
}

$latestCoverage = $coverageFiles[0].FullName
[xml]$coverageXml = Get-Content $latestCoverage -Raw

$invariant = [System.Globalization.CultureInfo]::InvariantCulture

$overallLineRate = [double]::Parse($coverageXml.coverage.'line-rate', $invariant)
$overallBranchRate = [double]::Parse($coverageXml.coverage.'branch-rate', $invariant)
$linesCovered = [int]$coverageXml.coverage.'lines-covered'
$linesValid = [int]$coverageXml.coverage.'lines-valid'

Write-Host ""
Write-Host "Coverage file: $latestCoverage" -ForegroundColor DarkGray
Write-Host ("Overall line coverage   : {0:N2}% ({1}/{2})" -f ($overallLineRate * 100), $linesCovered, $linesValid) -ForegroundColor Green
Write-Host ("Overall branch coverage : {0:N2}%" -f ($overallBranchRate * 100)) -ForegroundColor Green
Write-Host ""

$packages = @($coverageXml.coverage.packages.package)
if ($packages.Count -gt 0) {
    Write-Host "Per-package line coverage:" -ForegroundColor Cyan
    foreach ($pkg in $packages) {
        $pkgRate = [double]::Parse($pkg.'line-rate', $invariant)
        Write-Host ("  - {0}: {1:N2}%" -f $pkg.name, ($pkgRate * 100))
    }
    Write-Host ""
}

if ($overallLineRate -lt $MinimumLineRate) {
    $required = $MinimumLineRate * 100
    $actual = $overallLineRate * 100
    throw ("Coverage gate failed. Required >= {0:N2}%, actual {1:N2}%." -f $required, $actual)
}

Write-Host ("Coverage gate passed (>= {0:N2}%)." -f ($MinimumLineRate * 100)) -ForegroundColor Green
