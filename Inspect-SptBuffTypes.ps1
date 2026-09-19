param(
    [string] $RuntimeDirectory = (Join-Path $PSScriptRoot "..\spt-read-only\SPP-Tarkov\SPT_Runtime")
)

$resolvedRuntime = Resolve-Path $RuntimeDirectory -ErrorAction Stop
$assemblyFiles = Get-ChildItem $resolvedRuntime -Filter "*.dll"

if ($assemblyFiles.Count -eq 0)
{
    throw "No DLL files were found in '$resolvedRuntime'."
}

$cecilPath = Join-Path $resolvedRuntime "Mono.Cecil.dll"
if (-not (Test-Path $cecilPath -PathType Leaf))
{
    throw "Mono.Cecil.dll was not found at '$cecilPath'."
}

[void] [Reflection.Assembly]::LoadFrom($cecilPath)

function Get-CecilTypes
{
    param($Types)

    foreach ($typeDefinition in $Types)
    {
        $typeDefinition

        if ($typeDefinition.HasNestedTypes)
        {
            Get-CecilTypes $typeDefinition.NestedTypes
        }
    }
}

$matches = foreach ($assemblyFile in $assemblyFiles)
{
    $assemblyDefinition = $null

    try
    {
        $assemblyDefinition = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyFile.FullName)

        foreach ($availableType in Get-CecilTypes $assemblyDefinition.MainModule.Types)
        {
            $propertyNames = @($availableType.Properties | ForEach-Object { $_.Name })
            $hasBuffName = $availableType.Name -like "*Buff*"
        $hasBuffProperties =
            ($propertyNames -contains "BuffType") -or
            ($propertyNames -contains "Stimulator") -or
            ($propertyNames -contains "Health") -or
            ($propertyNames -contains "Effects") -or
            (($propertyNames -contains "AbsoluteValue") -and ($propertyNames -contains "Duration"))

            if ($hasBuffName -or $hasBuffProperties)
            {
                [PSCustomObject]@{
                    Assembly = $assemblyDefinition.Name.Name
                    Type = $availableType.FullName.Replace("/", "+")
                }
            }
        }
    }
    catch
    {
        # Native DLLs and unsupported images do not contain managed model metadata.
    }
    finally
    {
        if ($null -ne $assemblyDefinition)
        {
            $assemblyDefinition.Dispose()
        }
    }
}

if ($matches.Count -eq 0)
{
    Write-Warning "No buff-related exported types were found in '$resolvedRuntime'."
}
else
{
    $matches | Sort-Object Assembly, Type -Unique | Format-Table -AutoSize
}
