using Microsoft.Extensions.Logging;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>Registers validated custom-item hideout recipes with SPT.</summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class CraftingService
{
    private static readonly IReadOnlyDictionary<string, HideoutAreas> Benches =
        new Dictionary<string, HideoutAreas>(StringComparer.OrdinalIgnoreCase)
        {
            ["medStation"] = HideoutAreas.MedStation,
            ["kitchen"] = HideoutAreas.Kitchen,
            ["heating"] = HideoutAreas.Heating,
            ["waterCollector"] = HideoutAreas.WaterCollector,
            ["workbench"] = HideoutAreas.Workbench,
            ["intelligenceCenter"] = HideoutAreas.IntelligenceCenter
        };

    private readonly ILogger<CraftingService> _logger;
    private readonly HideoutTable _hideoutTable;
    private readonly TemplateTable _templateTable;
    private readonly StimIdService _idService;

    public CraftingService(ILogger<CraftingService> logger, HideoutTable hideoutTable, TemplateTable templateTable, StimIdService idService)
    {
        _logger = logger;
        _hideoutTable = hideoutTable;
        _templateTable = templateTable;
        _idService = idService;
    }

    public bool Register(ICraftableDefinition definition, string outputTemplateId)
    {
        CraftDefinition? craft = definition.Craft;
        if (craft?.Enabled != true) return true;

        if (!TryResolveBench(craft.Bench, out HideoutAreas bench))
        {
            _logger.LogError("[MoarSupplies] Craft for '{DefinitionId}' names unsupported bench '{Bench}'.", definition.Id, craft.Bench);
            return false;
        }

        foreach (CraftIngredientDefinition ingredient in craft.Ingredients)
        {
            if (!_templateTable.Items.ContainsKey(ingredient.TemplateId))
            {
                _logger.LogError("[MoarSupplies] Craft for '{DefinitionId}' references missing ingredient template '{TemplateId}'.", definition.Id, ingredient.TemplateId);
                return false;
            }
        }

        string recipeId = _idService.CreateCraftRecipe(definition.Id);
        if (_hideoutTable.Production.Recipes?.Any(recipe => string.Equals(recipe.Id.ToString(), recipeId, StringComparison.OrdinalIgnoreCase)) == true)
        {
            _logger.LogError("[MoarSupplies] Generated craft recipe ID '{RecipeId}' for '{DefinitionId}' is already registered.", recipeId, definition.Id);
            return false;
        }

        List<Requirement> requirements =
        [
            new Requirement { AreaType = (int)bench, RequiredLevel = craft.BenchLevel, Type = "Area" }
        ];
        requirements.AddRange(craft.Ingredients.Select(ingredient => new Requirement
        {
            TemplateId = ingredient.TemplateId,
            Count = ingredient.Count,
            IsEncoded = false,
            IsFunctional = false,
            IsSpawnedInSession = false,
            Type = "Item"
        }));

        HideoutProduction recipe = new()
        {
            Id = recipeId,
            AreaType = bench,
            Requirements = requirements,
            ProductionTime = craft.DurationSeconds,
            EndProduct = outputTemplateId,
            IsEncoded = false,
            Locked = false,
            NeedFuelForAllProductionTime = craft.RequiresFuel,
            Continuous = false,
            Count = craft.OutputCount,
            ProductionLimitCount = 0,
            IsCodeProduction = false
        };

        _hideoutTable.Production.Recipes ??= [];
        _hideoutTable.Production.Recipes.Add(recipe);
        _logger.LogInformation("[MoarSupplies] Registered {Bench} craft '{DefinitionId}' ({RecipeId}).", craft.Bench, definition.Id, recipeId);
        return true;
    }

    public static bool IsSupportedBench(string bench) => TryResolveBench(bench, out _);

    private static bool TryResolveBench(string bench, out HideoutAreas area) => Benches.TryGetValue(bench, out area);
}
