[CmdletBinding()]
param(
    [string] $SptRuntimeDirectory = (Join-Path $PSScriptRoot "..\..\SPP\SPP-Tarkov\SPT_Runtime")
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "MoarSupplies.csproj"
$runtimePath = (Resolve-Path -LiteralPath $SptRuntimeDirectory).Path
if (-not $runtimePath.EndsWith([System.IO.Path]::DirectorySeparatorChar))
{
    $runtimePath += [System.IO.Path]::DirectorySeparatorChar
}
$modDirectoryName = "AnotherBudgetGamer-MoarSupplies"
$buildOutputPath = Join-Path $PSScriptRoot "bin\Release\$modDirectoryName"
$destinationPath = Join-Path $runtimePath "user\mods\$modDirectoryName"

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf))
{
    throw "Moar Supplies project file was not found at '$projectPath'."
}

& dotnet build $projectPath -c Release "-p:SptRuntimeDirectory=$runtimePath"
if ($LASTEXITCODE -ne 0)
{
    throw "Moar Supplies release build failed."
}

if (-not (Test-Path -LiteralPath $buildOutputPath -PathType Container))
{
    throw "Moar Supplies build output was not found at '$buildOutputPath'."
}

New-Item -ItemType Directory -Path $destinationPath -Force | Out-Null
Get-ChildItem -LiteralPath $buildOutputPath -File | Copy-Item -Destination $destinationPath -Force

$sourceWebRoot = Join-Path $buildOutputPath "wwwroot"
if (Test-Path -LiteralPath $sourceWebRoot -PathType Container)
{
    Copy-Item -LiteralPath $sourceWebRoot -Destination $destinationPath -Recurse -Force
}

$destinationConfig = Join-Path $destinationPath "config"
$configWasPresent = Test-Path -LiteralPath $destinationConfig -PathType Container
if (-not $configWasPresent)
{
    Copy-Item -LiteralPath (Join-Path $buildOutputPath "config") -Destination $destinationPath -Recurse
}

Write-Host "Moar Supplies deployed to '$destinationPath'."
Write-Host "Existing config/ was preserved: $configWasPresent"
