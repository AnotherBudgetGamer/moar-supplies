using Microsoft.Extensions.Logging;
using MoarSupplies.Models;
using MoarSupplies.Validation;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using System.Text.Json;

namespace MoarSupplies.Services;

/// <summary>
/// Reads and writes the file-per-stim configuration format.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class ConfigStorage
{
    private const string ConfigDirectoryName = "config";
    private const string SettingsFileName = "settings.json";
    private const string StimDirectoryName = "stims";
    private const string DrinkDirectoryName = "drinks";
    private const string FoodDirectoryName = "foods";
    private const string MedicalPackDirectoryName = "medical-packs";
    private const string LegacyFileName = "stims.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly ILogger<ConfigStorage> _logger;
    private readonly ConfigValidator _configValidator;
    private readonly ConfigState _configState;

    public ConfigStorage(ILogger<ConfigStorage> logger, ConfigValidator configValidator, ConfigState configState)
    {
        _logger = logger;
        _configValidator = configValidator;
        _configState = configState;
    }

    public async Task<ConfigLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        string configDirectory = GetConfigDirectory();
        string stimDirectory = Path.Combine(configDirectory, StimDirectoryName);
        string[] definitionPaths = Directory.Exists(stimDirectory)
            ? Directory.GetFiles(stimDirectory, "*.json", SearchOption.TopDirectoryOnly)
            : [];
        string drinkDirectory = Path.Combine(configDirectory, DrinkDirectoryName);
        string[] drinkDefinitionPaths = Directory.Exists(drinkDirectory)
            ? Directory.GetFiles(drinkDirectory, "*.json", SearchOption.TopDirectoryOnly)
            : [];
        string foodDirectory = Path.Combine(configDirectory, FoodDirectoryName);
        string[] foodDefinitionPaths = Directory.Exists(foodDirectory)
            ? Directory.GetFiles(foodDirectory, "*.json", SearchOption.TopDirectoryOnly)
            : [];
        string medicalPackDirectory = Path.Combine(configDirectory, MedicalPackDirectoryName);
        string[] medicalPackDefinitionPaths = Directory.Exists(medicalPackDirectory)
            ? Directory.GetFiles(medicalPackDirectory, "*.json", SearchOption.TopDirectoryOnly)
            : [];

        if (definitionPaths.Length == 0 && drinkDefinitionPaths.Length == 0 && foodDefinitionPaths.Length == 0 && medicalPackDefinitionPaths.Length == 0)
        {
            string legacyPath = Path.Combine(configDirectory, LegacyFileName);
            if (!File.Exists(legacyPath))
            {
                throw new FileNotFoundException("No stim definitions were found.", stimDirectory);
            }

            ModConfig? legacyConfig = await DeserializeAsync<ModConfig>(legacyPath, cancellationToken);
            if (legacyConfig is null)
            {
                throw new InvalidDataException($"Legacy configuration '{legacyPath}' could not be deserialized.");
            }

            return new ConfigLoadResult(legacyConfig, UsesLegacyFormat: true, legacyPath);
        }

        ModSettings settings = await LoadSettingsAsync(configDirectory, cancellationToken);
        List<StimDefinition> stims = [];

        foreach (string definitionPath in definitionPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            StimDefinition? stim = await DeserializeAsync<StimDefinition>(definitionPath, cancellationToken);
            if (stim is null)
            {
                throw new InvalidDataException($"Stim definition '{definitionPath}' could not be deserialized.");
            }

            stims.Add(stim);
        }

        List<DrinkDefinition> drinks = [];
        if (drinkDefinitionPaths.Length > 0)
        {
            foreach (string definitionPath in drinkDefinitionPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                DrinkDefinition? drink = await DeserializeAsync<DrinkDefinition>(definitionPath, cancellationToken);
                if (drink is null) throw new InvalidDataException($"Drink definition '{definitionPath}' could not be deserialized.");
                drinks.Add(drink);
            }
        }

        List<FoodDefinition> foods = [];
        foreach (string definitionPath in foodDefinitionPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            FoodDefinition? food = await DeserializeAsync<FoodDefinition>(definitionPath, cancellationToken);
            if (food is null) throw new InvalidDataException($"Food definition '{definitionPath}' could not be deserialized.");
            foods.Add(food);
        }

        List<MedicalPackDefinition> medicalPacks = [];
        foreach (string definitionPath in medicalPackDefinitionPaths.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            MedicalPackDefinition? medicalPack = await DeserializeAsync<MedicalPackDefinition>(definitionPath, cancellationToken);
            if (medicalPack is null) throw new InvalidDataException($"Medical-pack definition '{definitionPath}' could not be deserialized.");
            medicalPacks.Add(medicalPack);
        }

        return new ConfigLoadResult(new ModConfig { Version = settings.Version, Debug = settings.Debug, EnableMappedItemTestClones = settings.EnableMappedItemTestClones, Stims = stims, Drinks = drinks, Foods = foods, MedicalPacks = medicalPacks }, UsesLegacyFormat: false, configDirectory);
    }

    public async Task<ConfigSaveResult> SaveAsync(StimDefinition definition, string? originalId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        if (current is null)
        {
            return ConfigSaveResult.Failure("A valid configuration must be loaded before a definition can be saved.");
        }

        List<StimDefinition> updatedStims = current.Stims
            .Where(stim => !string.Equals(stim.Id, originalId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        updatedStims.Add(definition);

        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = updatedStims, Drinks = current.Drinks, Foods = current.Foods, MedicalPacks = current.MedicalPacks };
        IReadOnlyList<string> validationErrors = _configValidator.Validate(updatedConfig);
        if (validationErrors.Count > 0)
        {
            return ConfigSaveResult.Failure(validationErrors);
        }

        try
        {
            await WriteSplitConfigurationAsync(updatedConfig, originalId, cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not save stim '{StimId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be written. Check the server log and that the config folder is writable.");
        }

        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> SaveDrinkAsync(DrinkDefinition definition, string? originalId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        if (current is null) return ConfigSaveResult.Failure("A valid configuration must be loaded before a definition can be saved.");

        List<DrinkDefinition> updatedDrinks = current.Drinks.Where(drink => !string.Equals(drink.Id, originalId, StringComparison.OrdinalIgnoreCase)).ToList();
        updatedDrinks.Add(definition);
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = current.Stims, Drinks = updatedDrinks, Foods = current.Foods, MedicalPacks = current.MedicalPacks };
        IReadOnlyList<string> validationErrors = _configValidator.Validate(updatedConfig);
        if (validationErrors.Count > 0) return ConfigSaveResult.Failure(validationErrors);

        try
        {
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);
            if (!string.IsNullOrWhiteSpace(originalId) && !string.Equals(originalId, definition.Id, StringComparison.OrdinalIgnoreCase))
            {
                string oldPath = Path.Combine(GetConfigDirectory(), DrinkDirectoryName, $"{originalId}.json");
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not save drink '{DrinkId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be written. Check the server log and that the config folder is writable.");
        }

        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> DeleteAsync(string stimId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        StimDefinition? definition = current?.Stims.FirstOrDefault(stim => string.Equals(stim.Id, stimId, StringComparison.OrdinalIgnoreCase));
        if (current is null || definition is null)
        {
            return ConfigSaveResult.Failure("The selected definition could not be found.");
        }

        List<StimDefinition> remainingStims = current.Stims
            .Where(stim => !string.Equals(stim.Id, definition.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = remainingStims, Drinks = current.Drinks, Foods = current.Foods, MedicalPacks = current.MedicalPacks };

        try
        {
            string configDirectory = GetConfigDirectory();
            string stimDirectory = Path.Combine(configDirectory, StimDirectoryName);
            Directory.CreateDirectory(stimDirectory);

            await WriteJsonAtomicallyAsync(Path.Combine(configDirectory, SettingsFileName), new ModSettings { Version = updatedConfig.Version, Debug = updatedConfig.Debug, EnableMappedItemTestClones = updatedConfig.EnableMappedItemTestClones }, cancellationToken);
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);

            string definitionPath = Path.Combine(stimDirectory, $"{definition.Id}.json");
            if (File.Exists(definitionPath))
            {
                File.Delete(definitionPath);
            }

            // A legacy file would otherwise restore the deleted definition when it was the final entry.
            string legacyPath = Path.Combine(configDirectory, LegacyFileName);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not delete stim '{StimId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be deleted. Check the server log and that the config folder is writable.");
        }

        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> SaveFoodAsync(FoodDefinition definition, string? originalId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        if (current is null) return ConfigSaveResult.Failure("A valid configuration must be loaded before a definition can be saved.");
        List<FoodDefinition> updatedFoods = current.Foods.Where(food => !string.Equals(food.Id, originalId, StringComparison.OrdinalIgnoreCase)).ToList();
        updatedFoods.Add(definition);
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = current.Stims, Drinks = current.Drinks, Foods = updatedFoods, MedicalPacks = current.MedicalPacks };
        IReadOnlyList<string> validationErrors = _configValidator.Validate(updatedConfig);
        if (validationErrors.Count > 0) return ConfigSaveResult.Failure(validationErrors);
        try
        {
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);
            if (!string.IsNullOrWhiteSpace(originalId) && !string.Equals(originalId, definition.Id, StringComparison.OrdinalIgnoreCase))
            {
                string oldPath = Path.Combine(GetConfigDirectory(), FoodDirectoryName, $"{originalId}.json");
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not save food '{FoodId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be written. Check the server log and that the config folder is writable.");
        }
        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> DeleteFoodAsync(string foodId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        FoodDefinition? definition = current?.Foods.FirstOrDefault(food => string.Equals(food.Id, foodId, StringComparison.OrdinalIgnoreCase));
        if (current is null || definition is null) return ConfigSaveResult.Failure("The selected definition could not be found.");
        List<FoodDefinition> remainingFoods = current.Foods.Where(food => !string.Equals(food.Id, definition.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = current.Stims, Drinks = current.Drinks, Foods = remainingFoods, MedicalPacks = current.MedicalPacks };
        try
        {
            string foodDirectory = Path.Combine(GetConfigDirectory(), FoodDirectoryName);
            Directory.CreateDirectory(foodDirectory);
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);
            string definitionPath = Path.Combine(foodDirectory, $"{definition.Id}.json");
            if (File.Exists(definitionPath)) File.Delete(definitionPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not delete food '{FoodId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be deleted. Check the server log and that the config folder is writable.");
        }
        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> SaveMedicalPackAsync(MedicalPackDefinition definition, string? originalId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        if (current is null) return ConfigSaveResult.Failure("A valid configuration must be loaded before a definition can be saved.");

        List<MedicalPackDefinition> updatedMedicalPacks = current.MedicalPacks.Where(medicalPack => !string.Equals(medicalPack.Id, originalId, StringComparison.OrdinalIgnoreCase)).ToList();
        updatedMedicalPacks.Add(definition);
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = current.Stims, Drinks = current.Drinks, Foods = current.Foods, MedicalPacks = updatedMedicalPacks };
        IReadOnlyList<string> validationErrors = _configValidator.Validate(updatedConfig);
        if (validationErrors.Count > 0) return ConfigSaveResult.Failure(validationErrors);

        try
        {
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);
            if (!string.IsNullOrWhiteSpace(originalId) && !string.Equals(originalId, definition.Id, StringComparison.OrdinalIgnoreCase))
            {
                string oldPath = Path.Combine(GetConfigDirectory(), MedicalPackDirectoryName, $"{originalId}.json");
                if (File.Exists(oldPath)) File.Delete(oldPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not save medical pack '{MedicalPackId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be written. Check the server log and that the config folder is writable.");
        }

        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> DeleteDrinkAsync(string drinkId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        DrinkDefinition? definition = current?.Drinks.FirstOrDefault(drink => string.Equals(drink.Id, drinkId, StringComparison.OrdinalIgnoreCase));
        if (current is null || definition is null)
        {
            return ConfigSaveResult.Failure("The selected definition could not be found.");
        }

        List<DrinkDefinition> remainingDrinks = current.Drinks
            .Where(drink => !string.Equals(drink.Id, definition.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = current.Stims, Drinks = remainingDrinks, Foods = current.Foods, MedicalPacks = current.MedicalPacks };

        try
        {
            string configDirectory = GetConfigDirectory();
            string drinkDirectory = Path.Combine(configDirectory, DrinkDirectoryName);
            Directory.CreateDirectory(drinkDirectory);

            await WriteJsonAtomicallyAsync(Path.Combine(configDirectory, SettingsFileName), new ModSettings { Version = updatedConfig.Version, Debug = updatedConfig.Debug, EnableMappedItemTestClones = updatedConfig.EnableMappedItemTestClones }, cancellationToken);
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);

            string definitionPath = Path.Combine(drinkDirectory, $"{definition.Id}.json");
            if (File.Exists(definitionPath))
            {
                File.Delete(definitionPath);
            }

            // A legacy file would otherwise restore the deleted definition when it was the final entry.
            string legacyPath = Path.Combine(configDirectory, LegacyFileName);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not delete drink '{DrinkId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be deleted. Check the server log and that the config folder is writable.");
        }

        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    public async Task<ConfigSaveResult> DeleteMedicalPackAsync(string medicalPackId, CancellationToken cancellationToken)
    {
        ModConfig? current = _configState.Current;
        MedicalPackDefinition? definition = current?.MedicalPacks.FirstOrDefault(medicalPack => string.Equals(medicalPack.Id, medicalPackId, StringComparison.OrdinalIgnoreCase));
        if (current is null || definition is null) return ConfigSaveResult.Failure("The selected definition could not be found.");

        List<MedicalPackDefinition> remainingMedicalPacks = current.MedicalPacks.Where(medicalPack => !string.Equals(medicalPack.Id, definition.Id, StringComparison.OrdinalIgnoreCase)).ToList();
        ModConfig updatedConfig = new() { Version = current.Version, Debug = current.Debug, EnableMappedItemTestClones = current.EnableMappedItemTestClones, Stims = current.Stims, Drinks = current.Drinks, Foods = current.Foods, MedicalPacks = remainingMedicalPacks };

        try
        {
            string configDirectory = GetConfigDirectory();
            string medicalPackDirectory = Path.Combine(configDirectory, MedicalPackDirectoryName);
            Directory.CreateDirectory(medicalPackDirectory);

            await WriteJsonAtomicallyAsync(Path.Combine(configDirectory, SettingsFileName), new ModSettings { Version = updatedConfig.Version, Debug = updatedConfig.Debug, EnableMappedItemTestClones = updatedConfig.EnableMappedItemTestClones }, cancellationToken);
            await WriteSplitConfigurationAsync(updatedConfig, cancellationToken);

            string definitionPath = Path.Combine(medicalPackDirectory, $"{definition.Id}.json");
            if (File.Exists(definitionPath)) File.Delete(definitionPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogError(exception, "[MoarSupplies] Could not delete medical pack '{MedicalPackId}'.", definition.Id);
            return ConfigSaveResult.Failure("The definition could not be deleted. Check the server log and that the config folder is writable.");
        }

        _configState.Current = updatedConfig;
        return ConfigSaveResult.Success();
    }

    private async Task WriteSplitConfigurationAsync(ModConfig config, string? originalId, CancellationToken cancellationToken)
    {
        await WriteSplitConfigurationAsync(config, cancellationToken);

        if (!string.IsNullOrWhiteSpace(originalId) && config.Stims.Count > 0 && !string.Equals(originalId, config.Stims.Last().Id, StringComparison.OrdinalIgnoreCase))
        {
            string oldDefinitionPath = Path.Combine(GetConfigDirectory(), StimDirectoryName, $"{originalId}.json");
            if (File.Exists(oldDefinitionPath)) File.Delete(oldDefinitionPath);
        }
    }

    private async Task WriteSplitConfigurationAsync(ModConfig config, CancellationToken cancellationToken)
    {
        string configDirectory = GetConfigDirectory();
        string stimDirectory = Path.Combine(configDirectory, StimDirectoryName);
        Directory.CreateDirectory(stimDirectory);

        await WriteJsonAtomicallyAsync(Path.Combine(configDirectory, SettingsFileName), new ModSettings { Version = config.Version, Debug = config.Debug, EnableMappedItemTestClones = config.EnableMappedItemTestClones }, cancellationToken);

        // On the first save from a legacy install, this writes every loaded definition so none are lost.
        foreach (StimDefinition stim in config.Stims)
        {
            await WriteJsonAtomicallyAsync(Path.Combine(stimDirectory, $"{stim.Id}.json"), stim, cancellationToken);
        }
        string drinkDirectory = Path.Combine(configDirectory, DrinkDirectoryName);
        Directory.CreateDirectory(drinkDirectory);
        foreach (DrinkDefinition drink in config.Drinks)
        {
            await WriteJsonAtomicallyAsync(Path.Combine(drinkDirectory, $"{drink.Id}.json"), drink, cancellationToken);
        }
        string foodDirectory = Path.Combine(configDirectory, FoodDirectoryName);
        Directory.CreateDirectory(foodDirectory);
        foreach (FoodDefinition food in config.Foods)
        {
            await WriteJsonAtomicallyAsync(Path.Combine(foodDirectory, $"{food.Id}.json"), food, cancellationToken);
        }
        string medicalPackDirectory = Path.Combine(configDirectory, MedicalPackDirectoryName);
        Directory.CreateDirectory(medicalPackDirectory);
        foreach (MedicalPackDefinition medicalPack in config.MedicalPacks)
        {
            await WriteJsonAtomicallyAsync(Path.Combine(medicalPackDirectory, $"{medicalPack.Id}.json"), medicalPack, cancellationToken);
        }
    }

    private static async Task<ModSettings> LoadSettingsAsync(string configDirectory, CancellationToken cancellationToken)
    {
        string settingsPath = Path.Combine(configDirectory, SettingsFileName);
        if (!File.Exists(settingsPath))
        {
            return new ModSettings();
        }

        return await DeserializeAsync<ModSettings>(settingsPath, cancellationToken)
            ?? throw new InvalidDataException($"Settings file '{settingsPath}' could not be deserialized.");
    }

    private static async Task<T?> DeserializeAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static async Task WriteJsonAtomicallyAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        string temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string GetConfigDirectory()
    {
        string assemblyDirectory = Path.GetDirectoryName(typeof(ConfigStorage).Assembly.Location) ?? AppContext.BaseDirectory;
        return Path.Combine(assemblyDirectory, ConfigDirectoryName);
    }
}

public sealed record ConfigLoadResult(ModConfig Config, bool UsesLegacyFormat, string SourcePath);

public sealed record ConfigSaveResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static ConfigSaveResult Success() => new(true, []);
    public static ConfigSaveResult Failure(string error) => new(false, [error]);
    public static ConfigSaveResult Failure(IReadOnlyList<string> errors) => new(false, errors);
}
