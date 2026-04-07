param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repoRoot "UnitTests/UnitTests.csproj"
$coverageDir = Join-Path $repoRoot "coverage"
$reportDir = Join-Path $repoRoot "coveragereport"
$coverageFile = Join-Path $coverageDir "coverage.cobertura.xml"
$testDll = Join-Path $repoRoot "UnitTests/bin/$Configuration/net8.0/UnitTests.dll"
$globalToolsDir = Join-Path $HOME ".dotnet/tools"
$coverletExe = Join-Path $globalToolsDir "coverlet.exe"
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
Ensure-DotnetTool -PackageId "coverlet.console"
Ensure-DotnetTool -PackageId "dotnet-reportgenerator-globaltool"

if (-not (Test-Path $coverletExe)) {
    throw "coverlet executable not found at '$coverletExe'. Ensure global dotnet tools are installed correctly."
}

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
New-Item -ItemType Directory -Path $coverageDir | Out-Null

Write-Host "Building test project..."
dotnet build $testProject -c $Configuration -v minimal | Out-Host

Write-Host "Running tests with coverlet..."
& $coverletExe $testDll `
    --target "dotnet" `
    --targetargs "test $testProject -c $Configuration --no-build -v minimal" `
    --format "cobertura" `
    --output $coverageFile

Write-Host "Generating HTML coverage report..."
& $reportGeneratorExe `
    "-reports:$coverageFile" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;HtmlSummary"

Write-Host "Coverage XML: $coverageFile"
Write-Host "Coverage HTML: $reportDir/index.html"
