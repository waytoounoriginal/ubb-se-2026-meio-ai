param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "UnitTests/UnitTests.csproj"
$coverageDir = Join-Path $repoRoot "coverage"
$reportDir = Join-Path $repoRoot "coveragereport"
$coverageFile = Join-Path $coverageDir "coverage.cobertura.xml"
$testResultsDir = Join-Path $repoRoot "UnitTests/TestResults"
$globalToolsDir = Join-Path $HOME ".dotnet/tools"
$reportGeneratorExe = Join-Path $globalToolsDir "reportgenerator.exe"

function Ensure-DotnetTool {
    param(
        [Parameter(Mandatory = $true)][string]$PackageId
    )

    $globalTools = dotnet tool list -g | Out-String
    if ($globalTools -notmatch [regex]::Escape($PackageId)) {
        Write-Host "Installing global tool '$PackageId'..."
        dotnet tool install -g $PackageId | Out-Host
    }
}

Write-Host "Checking required global tools..."
Ensure-DotnetTool -PackageId "dotnet-reportgenerator-globaltool"

if (-not (Test-Path $reportGeneratorExe)) {
    throw "reportgenerator executable not found at '$reportGeneratorExe'. Ensure global dotnet tools are installed correctly."
}

Write-Host "Preparing coverage output folders..."
if (Test-Path $coverageDir) {
    Remove-Item -Recurse -Force $coverageDir
}
if (Test-Path $reportDir) {
    Remove-Item -Recurse -Force $reportDir
}
if (Test-Path $testResultsDir) {
    Remove-Item -Recurse -Force $testResultsDir
}
New-Item -ItemType Directory -Path $coverageDir | Out-Null

Write-Host "Building test project..."
dotnet build $testProject -c $Configuration -v minimal | Out-Host

Write-Host "Running tests with coverage collector..."
dotnet test $testProject `
    -c $Configuration `
    --no-build `
    --results-directory $testResultsDir `
    --collect:"XPlat Code Coverage" `
    -v minimal | Out-Host

$coverageCandidate = Get-ChildItem -Path $testResultsDir -Filter "coverage.cobertura.xml" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $coverageCandidate) {
    throw "coverage.cobertura.xml was not generated under '$testResultsDir'."
}

Copy-Item -Path $coverageCandidate -Destination $coverageFile -Force

Write-Host "Generating HTML coverage report..."
& $reportGeneratorExe `
    "-reports:$coverageFile" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;HtmlSummary"

Write-Host "Coverage XML: $coverageFile"
Write-Host "Coverage HTML: $reportDir/index.html"
