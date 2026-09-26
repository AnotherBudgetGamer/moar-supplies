using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using MoarSupplies.Validation;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace MoarSupplies.Services;

/// <summary>
/// Loads the user-facing configuration after SPT has constructed its dependency-injection container.
/// </summary>
[Injectable(InjectionType.Singleton, OnLoadOrder.TraderRegistration + 1)]
public sealed class ConfigLoader : IOnLoad
{
    private readonly ILogger<ConfigLoader> _logger;
    private readonly ConfigValidator _configValidator;
    private readonly DebugSettings _debugSettings;
    private readonly ConfigState _configState;
    private readonly ConfigStorage _configStorage;
    private readonly StimService _stimService;
    private readonly DrinkService _drinkService;
    private readonly FoodService _foodService;
    private readonly MedicalPackService _medicalPackService;
    private readonly MappedItemTestCloneService _mappedItemTestCloneService;

    public ConfigLoader(
        ILogger<ConfigLoader> logger,
        ConfigValidator configValidator,
        DebugSettings debugSettings,
        ConfigState configState,
        ConfigStorage configStorage,
        StimService stimService,
        DrinkService drinkService,
        FoodService foodService,
        MedicalPackService medicalPackService,
        MappedItemTestCloneService mappedItemTestCloneService)
    {
        _logger = logger;
        _configValidator = configValidator;
        _debugSettings = debugSettings;
        _configState = configState;
        _configStorage = configStorage;
        _stimService = stimService;
        _drinkService = drinkService;
        _foodService = foodService;
        _medicalPackService = medicalPackService;
        _mappedItemTestCloneService = mappedItemTestCloneService;
    }

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ConfigLoadResult loadResult;
        try
        {
            loadResult = await _configStorage.LoadAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
        {
            _logger.LogError("[MoarSupplies] Configuration could not be loaded: {Message}", exception.Message);
            return;
        }

        ModConfig config = loadResult.Config;
        _debugSettings.Enabled = config.Debug;
        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] Loading configuration from {ConfigPath}.{LegacySuffix}", loadResult.SourcePath, loadResult.UsesLegacyFormat ? " Legacy aggregate format will be migrated when you save a stim." : string.Empty);
            _logger.LogInformation("[MoarSupplies] Verbose debug logging is enabled.");
        }

        IReadOnlyList<string> validationErrors = _configValidator.Validate(config);
        if (validationErrors.Count > 0)
        {
            _logger.LogError("[MoarSupplies] Error loading {ErrorCount} definition validation error(s). No supplies will be registered.", validationErrors.Count);

            if (_debugSettings.Enabled)
            {
                foreach (string error in validationErrors)
                {
                    _logger.LogError("[MoarSupplies] {ValidationError}", error);
                }

                _logger.LogError(
                    "[MoarSupplies] Recovery: delete or correct the invalid definition record identified above in {ConfigPath}, then restart the SPT server.",
                    loadResult.SourcePath);
            }

            return;
        }

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] Configuration validation passed.");
        }

        _configState.Current = config;
        _logger.LogInformation("[MoarSupplies] Configuration loaded.");
        int totalDefinitionCount = config.Stims.Count + config.Drinks.Count + config.Foods.Count + config.MedicalPacks.Count;
        _logger.LogInformation("[MoarSupplies] Loaded {TotalDefinitionCount} total definition(s).", totalDefinitionCount);
        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] Loaded {StimCount} stim definition(s).", config.Stims.Count);
            _logger.LogInformation("[MoarSupplies] Loaded {DrinkCount} drink definition(s).", config.Drinks.Count);
            _logger.LogInformation("[MoarSupplies] Loaded {FoodCount} food definition(s).", config.Foods.Count);
            _logger.LogInformation("[MoarSupplies] Loaded {MedicalPackCount} medical-pack definition(s).", config.MedicalPacks.Count);
        }

        int createdStimCount = 0;
        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] ===== Stimulant definitions =====");
        }
        foreach (StimDefinition stim in config.Stims)
        {
            if (_debugSettings.Enabled)
            {
                ItemMappings.TryGet(stim.BaseItem, out BaseItemMapping baseItem);
                _logger.LogInformation(
                    "[MoarSupplies] Loading stim '{StimId}': name '{Name}', base '{BaseItem}' ({BaseTemplateId}), uses {Uses}, enabled {Enabled}, buffs {BuffCount}, tags [{Tags}], trader {Trader}.",
                    stim.Id, stim.Identity.Name, stim.BaseItem, baseItem.TemplateId, stim.Uses, stim.Enabled, stim.Buffs.Count, DescribeTags(stim.Tags), DescribeTrader(stim.Trader));
            }

            if (!stim.Enabled)
            {
                if (_debugSettings.Enabled)
                {
                    _logger.LogInformation("[MoarSupplies] Stim '{StimId}' is disabled; registration was skipped.", stim.Id);
                }

                continue;
            }

            if (!_stimService.Register(stim))
            {
                _logger.LogError("[MoarSupplies] Registration stopped after stim '{StimId}' failed.", stim.Id);
                return;
            }

            if (_debugSettings.Enabled)
            {
                _logger.LogInformation("[MoarSupplies] Registered stim '{StimId}' with {BuffCount} buff(s).", stim.Id, stim.Buffs.Count);
            }

            createdStimCount++;
        }

        int createdDrinkCount = 0;
        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] ===== Drink definitions =====");
        }
        foreach (DrinkDefinition drink in config.Drinks)
        {
            if (_debugSettings.Enabled)
            {
                ItemMappings.TryGetDrink(drink.BaseItem, out BaseItemMapping baseItem);
                _logger.LogInformation(
                    "[MoarSupplies] Loading drink '{DrinkId}': name '{Name}', base '{BaseItem}' ({BaseTemplateId}), resource {Resource}, hydration {Hydration}, energy {Energy}, enabled {Enabled}, buffs {BuffCount}, tags [{Tags}], trader {Trader}.",
                    drink.Id, drink.Identity.Name, drink.BaseItem, baseItem.TemplateId, drink.Resource, drink.Nutrition.Hydration, drink.Nutrition.Energy, drink.Enabled, drink.Buffs.Count, DescribeTags(drink.Tags), DescribeTrader(drink.Trader));
            }

            if (!drink.Enabled)
            {
                if (_debugSettings.Enabled) _logger.LogInformation("[MoarSupplies] Drink '{DrinkId}' is disabled; registration was skipped.", drink.Id);
                continue;
            }
            if (!_drinkService.Register(drink))
            {
                _logger.LogError("[MoarSupplies] Registration stopped after drink '{DrinkId}' failed.", drink.Id);
                return;
            }

            createdDrinkCount++;
        }

        int createdMedicalPackCount = 0;
        int createdFoodCount = 0;
        if (_debugSettings.Enabled) _logger.LogInformation("[MoarSupplies] ===== Food definitions =====");
        foreach (FoodDefinition food in config.Foods)
        {
            if (_debugSettings.Enabled)
            {
                ItemMappings.TryGetFood(food.BaseItem, out BaseItemMapping baseItem);
                _logger.LogInformation("[MoarSupplies] Loading food '{FoodId}': name '{Name}', base '{BaseItem}' ({BaseTemplateId}), resource {Resource}, hydration {Hydration}, energy {Energy}, enabled {Enabled}, tags [{Tags}], trader {Trader}.", food.Id, food.Identity.Name, food.BaseItem, baseItem.TemplateId, food.Resource, food.Nutrition.Hydration, food.Nutrition.Energy, food.Enabled, DescribeTags(food.Tags), DescribeTrader(food.Trader));
            }
            if (!food.Enabled)
            {
                if (_debugSettings.Enabled) _logger.LogInformation("[MoarSupplies] Food '{FoodId}' is disabled; registration was skipped.", food.Id);
                continue;
            }
            if (!_foodService.Register(food))
            {
                _logger.LogError("[MoarSupplies] Registration stopped after food '{FoodId}' failed.", food.Id);
                return;
            }
            createdFoodCount++;
        }

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] ===== Medical-pack definitions =====");
        }
        foreach (MedicalPackDefinition medicalPack in config.MedicalPacks)
        {
            if (_debugSettings.Enabled)
            {
                ItemMappings.TryGetMedicalPack(medicalPack.BaseItem, out BaseItemMapping baseItem);
                _logger.LogInformation(
                    "[MoarSupplies] Loading medical pack '{MedicalPackId}': name '{Name}', base '{BaseItem}' ({BaseTemplateId}), resource {Resource}, resource rate {ResourceRate}, use-time multiplier {UseTimeMultiplier}, surgery-restoration multiplier {SurgeryRestoreMultiplier}, enabled {Enabled}, tags [{Tags}], trader {Trader}.",
                    medicalPack.Id, medicalPack.Identity.Name, medicalPack.BaseItem, baseItem.TemplateId, medicalPack.Resource, medicalPack.ResourceRate, medicalPack.UseTimeMultiplier ?? 1, medicalPack.SurgeryRestoreMultiplier ?? 1, medicalPack.Enabled, DescribeTags(medicalPack.Tags), DescribeTrader(medicalPack.Trader));
            }

            if (!medicalPack.Enabled)
            {
                if (_debugSettings.Enabled) _logger.LogInformation("[MoarSupplies] Medical pack '{MedicalPackId}' is disabled; registration was skipped.", medicalPack.Id);
                continue;
            }
            if (!_medicalPackService.Register(medicalPack))
            {
                _logger.LogError("[MoarSupplies] Registration stopped after medical pack '{MedicalPackId}' failed.", medicalPack.Id);
                return;
            }

            createdMedicalPackCount++;
        }

        _logger.LogInformation("[MoarSupplies] {StimCount} stim(s) created.", createdStimCount);
        _logger.LogInformation("[MoarSupplies] {DrinkCount} drink(s) created.", createdDrinkCount);
        _logger.LogInformation("[MoarSupplies] {FoodCount} food item(s) created.", createdFoodCount);
        _logger.LogInformation("[MoarSupplies] {MedicalPackCount} medical pack(s) created.", createdMedicalPackCount);
        _mappedItemTestCloneService.CreateMissingMappedItemClones(config);
    }

    private static string DescribeTags(IEnumerable<string> tags) => tags.Any() ? string.Join(", ", tags) : "none";

    private static string DescribeTrader(TraderDefinition? trader) => trader?.Enabled == true
        ? $"{trader.Trader} (LL{trader.LoyaltyLevel}, {trader.Price} roubles)"
        : "disabled";
}
