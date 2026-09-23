using MoarSupplies.Definitions;
using MoarSupplies.Models;
using MoarSupplies.Services;
using SPTarkov.DI.Annotations;
using System.Text.RegularExpressions;

namespace MoarSupplies.Validation;

/// <summary>
/// Validates the entire user-facing configuration before later milestones modify SPT data.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed partial class ConfigValidator
{
    private const int SupportedConfigVersion = 1;

    private static readonly Regex SafeStimId = CreateSafeStimIdRegex();
    private readonly TraderDirectory _traderDirectory;

    public ConfigValidator(TraderDirectory traderDirectory)
    {
        _traderDirectory = traderDirectory;
    }

    public IReadOnlyList<string> Validate(ModConfig config)
    {
        List<string> errors = [];

        if (config.Version != SupportedConfigVersion)
        {
            errors.Add($"Configuration version '{config.Version}' is not supported. Expected version {SupportedConfigVersion}.");
        }

        if (config.Stims is null)
        {
            errors.Add("Configuration field 'stims' is required.");
            return errors;
        }

        HashSet<string> definitionIds = new(StringComparer.OrdinalIgnoreCase);

        for (int stimIndex = 0; stimIndex < config.Stims.Count; stimIndex++)
        {
            StimDefinition? stim = config.Stims[stimIndex];
            if (stim is null)
            {
                errors.Add($"stims[{stimIndex}] must be an object.");
                continue;
            }

            ValidateStim(stim, stimIndex, definitionIds, errors);
        }

        if (config.Drinks is null)
        {
            errors.Add("Configuration field 'drinks' is required.");
            return errors;
        }

        for (int drinkIndex = 0; drinkIndex < config.Drinks.Count; drinkIndex++)
        {
            DrinkDefinition? drink = config.Drinks[drinkIndex];
            if (drink is null)
            {
                errors.Add($"drinks[{drinkIndex}] must be an object.");
                continue;
            }

            ValidateDrink(drink, drinkIndex, definitionIds, errors);
        }

        if (config.MedicalPacks is null)
        {
            errors.Add("Configuration field 'medicalPacks' is required.");
            return errors;
        }

        for (int medicalPackIndex = 0; medicalPackIndex < config.MedicalPacks.Count; medicalPackIndex++)
        {
            MedicalPackDefinition? medicalPack = config.MedicalPacks[medicalPackIndex];
            if (medicalPack is null)
            {
                errors.Add($"medicalPacks[{medicalPackIndex}] must be an object.");
                continue;
            }

            ValidateMedicalPack(medicalPack, medicalPackIndex, definitionIds, errors);
        }

        return errors;
    }

    private void ValidateMedicalPack(MedicalPackDefinition medicalPack, int medicalPackIndex, HashSet<string> definitionIds, List<string> errors)
    {
        string label = string.IsNullOrWhiteSpace(medicalPack.Id) ? $"medicalPacks[{medicalPackIndex}]" : $"Medical pack '{medicalPack.Id}'";
        if (string.IsNullOrWhiteSpace(medicalPack.Id)) errors.Add($"{label}: field 'id' is required.");
        else
        {
            if (!SafeStimId.IsMatch(medicalPack.Id)) errors.Add($"{label}: field 'id' must use lowercase letters, numbers, and single hyphens only.");
            if (!definitionIds.Add(medicalPack.Id)) errors.Add($"{label}: field 'id' duplicates another definition ID.");
        }

        if (medicalPack.Identity is null) errors.Add($"{label}: field 'identity' is required.");
        else
        {
            if (string.IsNullOrWhiteSpace(medicalPack.Identity.Name)) errors.Add($"{label}: field 'identity.name' is required.");
            if (string.IsNullOrWhiteSpace(medicalPack.Identity.ShortName)) errors.Add($"{label}: field 'identity.shortName' is required.");
            if (string.IsNullOrWhiteSpace(medicalPack.Identity.Description)) errors.Add($"{label}: field 'identity.description' is required.");
        }

        if (string.IsNullOrWhiteSpace(medicalPack.BaseItem)) errors.Add($"{label}: field 'baseItem' is required.");
        else if (!ItemMappings.IsSupportedMedicalPack(medicalPack.BaseItem)) errors.Add($"{label}: field 'baseItem' value '{medicalPack.BaseItem}' is not supported.");
        if (medicalPack.Resource < 0) errors.Add($"{label}: field 'resource' must be zero or greater.");
        if (medicalPack.ResourceRate < 0) errors.Add($"{label}: field 'resourceRate' must be zero or greater.");
        if (medicalPack.UseTimeMultiplier is double useTimeMultiplier && (!double.IsFinite(useTimeMultiplier) || useTimeMultiplier <= 0)) errors.Add($"{label}: field 'useTimeMultiplier' must be a finite number greater than zero when provided.");
        if (medicalPack.SurgeryRestoreMultiplier is double surgeryRestoreMultiplier && (!double.IsFinite(surgeryRestoreMultiplier) || surgeryRestoreMultiplier <= 0)) errors.Add($"{label}: field 'surgeryRestoreMultiplier' must be a finite number greater than zero when provided.");
        ValidateTags(medicalPack.Tags, label, errors);
        ValidateTrader(medicalPack.Trader, label, errors);
    }

    private void ValidateDrink(DrinkDefinition drink, int drinkIndex, HashSet<string> definitionIds, List<string> errors)
    {
        string label = string.IsNullOrWhiteSpace(drink.Id) ? $"drinks[{drinkIndex}]" : $"Drink '{drink.Id}'";
        if (string.IsNullOrWhiteSpace(drink.Id)) errors.Add($"{label}: field 'id' is required.");
        else
        {
            if (!SafeStimId.IsMatch(drink.Id)) errors.Add($"{label}: field 'id' must use lowercase letters, numbers, and single hyphens only.");
            if (!definitionIds.Add(drink.Id)) errors.Add($"{label}: field 'id' duplicates another definition ID.");
        }

        if (drink.Identity is null) errors.Add($"{label}: field 'identity' is required.");
        else
        {
            if (string.IsNullOrWhiteSpace(drink.Identity.Name)) errors.Add($"{label}: field 'identity.name' is required.");
            if (string.IsNullOrWhiteSpace(drink.Identity.ShortName)) errors.Add($"{label}: field 'identity.shortName' is required.");
            if (string.IsNullOrWhiteSpace(drink.Identity.Description)) errors.Add($"{label}: field 'identity.description' is required.");
        }

        if (string.IsNullOrWhiteSpace(drink.BaseItem)) errors.Add($"{label}: field 'baseItem' is required.");
        else if (!ItemMappings.IsSupportedDrink(drink.BaseItem)) errors.Add($"{label}: field 'baseItem' value '{drink.BaseItem}' is not supported.");
        if (drink.Resource <= 0) errors.Add($"{label}: field 'resource' must be greater than zero.");
        if (drink.Nutrition is null) errors.Add($"{label}: field 'nutrition' is required.");
        else if (!double.IsFinite(drink.Nutrition.Hydration) || !double.IsFinite(drink.Nutrition.Energy)) errors.Add($"{label}: nutrition values must be finite numbers.");
        if (drink.Nutrition is not null && drink.Nutrition.Hydration == 0 && drink.Nutrition.Energy == 0 && (drink.Buffs is null || drink.Buffs.Count == 0)) errors.Add($"{label}: requires nutrition or at least one timed effect.");
        ValidateTags(drink.Tags, label, errors);
        ValidateBuffs(drink.Buffs, label, errors);
        ValidateTrader(drink.Trader, label, errors);
    }

    private void ValidateStim(StimDefinition stim, int stimIndex, HashSet<string> stimIds, List<string> errors)
    {
        string label = string.IsNullOrWhiteSpace(stim.Id) ? $"stims[{stimIndex}]" : $"Stim '{stim.Id}'";

        if (string.IsNullOrWhiteSpace(stim.Id))
        {
            errors.Add($"{label}: field 'id' is required.");
        }
        else
        {
            if (!SafeStimId.IsMatch(stim.Id))
            {
                errors.Add($"{label}: field 'id' must use lowercase letters, numbers, and single hyphens only.");
            }

            if (!stimIds.Add(stim.Id))
            {
                errors.Add($"{label}: field 'id' duplicates another stim ID.");
            }
        }

        if (stim.Identity is null)
        {
            errors.Add($"{label}: field 'identity' is required.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(stim.Identity.Name))
            {
                errors.Add($"{label}: field 'identity.name' is required.");
            }

            if (string.IsNullOrWhiteSpace(stim.Identity.ShortName))
            {
                errors.Add($"{label}: field 'identity.shortName' is required.");
            }

            if (string.IsNullOrWhiteSpace(stim.Identity.Description))
            {
                errors.Add($"{label}: field 'identity.description' is required.");
            }
        }

        if (string.IsNullOrWhiteSpace(stim.BaseItem))
        {
            errors.Add($"{label}: field 'baseItem' is required.");
        }
        else if (!ItemMappings.IsSupported(stim.BaseItem))
        {
            errors.Add($"{label}: field 'baseItem' value '{stim.BaseItem}' is not supported.");
        }

        if (stim.Uses <= 0)
        {
            errors.Add($"{label}: field 'uses' must be greater than zero.");
        }

        ValidateTags(stim, label, errors);
        ValidateBuffs(stim, label, errors);
        ValidateTrader(stim, label, errors);
    }

    private static void ValidateTags(StimDefinition stim, string label, List<string> errors) => ValidateTags(stim.Tags, label, errors);

    private static void ValidateTags(List<string>? tags, string label, List<string> errors)
    {
        if (tags is null)
        {
            errors.Add($"{label}: field 'tags' must be an array when provided.");
            return;
        }

        if (tags.Count > 8)
        {
            errors.Add($"{label}: field 'tags' may contain at most 8 tags.");
        }

        HashSet<string> uniqueTags = new(StringComparer.OrdinalIgnoreCase);
        for (int tagIndex = 0; tagIndex < tags.Count; tagIndex++)
        {
            string? tag = tags[tagIndex];
            string tagLabel = $"{label}: tags[{tagIndex}]";

            if (string.IsNullOrWhiteSpace(tag))
            {
                errors.Add($"{tagLabel} must not be blank.");
                continue;
            }

            if (tag.Length > 32)
            {
                errors.Add($"{tagLabel} must be 32 characters or fewer.");
            }

            if (!string.Equals(tag, tag.Trim(), StringComparison.Ordinal))
            {
                errors.Add($"{tagLabel} must not begin or end with whitespace.");
            }

            if (!uniqueTags.Add(tag))
            {
                errors.Add($"{tagLabel} duplicates another tag on this stim.");
            }
        }
    }

    private static void ValidateBuffs(StimDefinition stim, string label, List<string> errors) => ValidateBuffs(stim.Buffs, label, errors);

    private static void ValidateBuffs(List<BuffDefinition>? buffs, string label, List<string> errors)
    {
        if (buffs is null)
        {
            errors.Add($"{label}: field 'buffs' is required.");
            return;
        }

        for (int buffIndex = 0; buffIndex < buffs.Count; buffIndex++)
        {
            BuffDefinition? buff = buffs[buffIndex];
            string buffLabel = $"{label}: buffs[{buffIndex}]";

            if (buff is null)
            {
                errors.Add($"{buffLabel} must be an object.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(buff.Effect))
            {
                errors.Add($"{buffLabel}.effect is required.");
                continue;
            }

            if (!BuffMappings.TryGet(buff.Effect, out BuffMapping mapping))
            {
                errors.Add($"{buffLabel}.effect '{buff.Effect}' is not a supported effect.");
            }
            else if (mapping.RequiresValue && buff.Value is null)
            {
                errors.Add($"{buffLabel}.value is required for effect '{buff.Effect}'.");
            }

            if (buff.Value is double value && !double.IsFinite(value))
            {
                errors.Add($"{buffLabel}.value must be a finite number.");
            }

            if (buff.Duration < 0)
            {
                errors.Add($"{buffLabel}.duration must be zero or greater.");
            }
            else if (buff.Duration > EffectDuration.MaximumSeconds)
            {
                errors.Add($"{buffLabel}.duration must not exceed {EffectDuration.MaximumSeconds} seconds (30 minutes).");
            }

            if (buff.Delay < 0)
            {
                errors.Add($"{buffLabel}.delay must be zero or greater.");
            }
        }
    }

    private void ValidateTrader(StimDefinition stim, string label, List<string> errors) => ValidateTrader(stim.Trader, label, errors);

    private void ValidateTrader(TraderDefinition? traderDefinition, string label, List<string> errors)
    {
        if (traderDefinition?.Enabled != true)
        {
            return;
        }

        TraderDefinition trader = traderDefinition;

        if (string.IsNullOrWhiteSpace(trader.Trader))
        {
            errors.Add($"{label}: field 'trader.trader' is required when trader.enabled is true.");
        }
        else if (!_traderDirectory.TryResolve(trader, out _))
        {
            string selectedTrader = string.IsNullOrWhiteSpace(trader.TraderId) ? trader.Trader : $"{trader.Trader} ({trader.TraderId})";
            errors.Add($"{label}: trader '{selectedTrader}' was not found. Install and enable the trader mod before starting SPT, then select the trader again in the workshop.");
        }

        if (trader.LoyaltyLevel is < 1 or > 4)
        {
            errors.Add($"{label}: field 'trader.loyaltyLevel' must be between 1 and 4 when trader.enabled is true.");
        }

        if (trader.Price <= 0)
        {
            errors.Add($"{label}: field 'trader.price' must be greater than zero when trader.enabled is true.");
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex CreateSafeStimIdRegex();
}
