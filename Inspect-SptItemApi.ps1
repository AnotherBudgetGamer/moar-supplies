param(
    [string] $RuntimeDirectory = (Join-Path $PSScriptRoot "..\spt-read-only\SPP-Tarkov\SPT_Runtime")
)

$resolvedRuntime = Resolve-Path $RuntimeDirectory -ErrorAction Stop
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

function Format-Method
{
    param($Method)

    $parameters = @($Method.Parameters | ForEach-Object {
        "{0} {1}" -f $_.ParameterType.FullName, $_.Name
    }) -join ", "

    "  METHOD {0} {1}({2})" -f $Method.ReturnType.FullName, $Method.Name, $parameters
}

$namePatterns = @(
    "CustomItemService",
    "NewItemFromClone",
    "NewItemDetailsBase",
    "CreateItemResult",
    "LocaleDetails",
    "CreateItemFromClone",
    "Database",
    "TableService",
    "ItemHelper",
    "Models.Spt.Tables.GlobalConfig",
    "Models.Spt.Tables.Health",
    "Models.Spt.Tables.Effects",
    "Models.Spt.Tables.TemplateTable",
    "Models.Eft.Common.Tables.TemplateItem",
    "Models.Common.MongoId",
    "TemplateItemProperties",
    "TraderAssort",
    "CustomTraderService",
    "TraderHelper",
    "TraderTable",
    "TradersTable",
    "Models.Eft.Common.Tables.Trader",
    "Models.Eft.Common.Tables.Item",
    "Models.Eft.Common.Tables.Upd",
    "Models.Eft.Common.Tables.UpdMedKit",
    "Models.Eft.Common.Tables.UpdResource",
    "AssortHelper",
    "BarterScheme",
    "HandbookBase",
    "HandbookItem",
    "LoyalLevelItems",
    "AddItemToAssort",
    "IOnLoad",
    "OnLoadOrder",
    "Stimulator"
)

$matches = foreach ($assemblyFile in Get-ChildItem $resolvedRuntime -Filter "SPTarkov*.dll")
{
    $assemblyDefinition = $null

    try
    {
        $assemblyDefinition = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyFile.FullName)

        foreach ($availableType in Get-CecilTypes $assemblyDefinition.MainModule.Types)
        {
            $isMatch = $false

            foreach ($pattern in $namePatterns)
            {
                if ($availableType.Name -like "*$pattern*" -or $availableType.FullName -like "*$pattern*")
                {
                    $isMatch = $true
                    break
                }
            }

            if (-not $isMatch)
            {
                $referencesGlobalConfig =
                    @($availableType.Fields | Where-Object { $_.FieldType.FullName -eq "SPTarkov.Server.Core.Models.Spt.Tables.GlobalConfig" }).Count -gt 0 -or
                    @($availableType.Properties | Where-Object { $_.PropertyType.FullName -eq "SPTarkov.Server.Core.Models.Spt.Tables.GlobalConfig" }).Count -gt 0 -or
                    @($availableType.Methods | Where-Object {
                        $_.ReturnType.FullName -eq "SPTarkov.Server.Core.Models.Spt.Tables.GlobalConfig" -or
                        @($_.Parameters | Where-Object { $_.ParameterType.FullName -eq "SPTarkov.Server.Core.Models.Spt.Tables.GlobalConfig" }).Count -gt 0
                    }).Count -gt 0

                $isMatch = $referencesGlobalConfig
            }

            if (-not $isMatch)
            {
                continue
            }

            "TYPE {0} [{1}]" -f $availableType.FullName.Replace("/", "+"), $assemblyDefinition.Name.Name

            foreach ($property in $availableType.Properties)
            {
                "  PROPERTY {0} {1}" -f $property.PropertyType.FullName, $property.Name
            }

            foreach ($field in $availableType.Fields)
            {
                $constant = if ($field.HasConstant) { " = {0}" -f $field.Constant } else { "" }
                "  FIELD {0} {1}{2}" -f $field.FieldType.FullName, $field.Name, $constant
            }

            foreach ($method in $availableType.Methods | Where-Object { $_.IsPublic -or $_.IsConstructor })
            {
                Format-Method $method
            }

            ""
        }
    }
    catch
    {
        # Ignore non-managed or otherwise unsupported images.
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
    Write-Warning "No matching SPT item-registration API types were found in '$resolvedRuntime'."
}
else
{
    $matches
}
