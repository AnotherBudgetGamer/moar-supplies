using MoarSupplies.Definitions;
using MoarSupplies.Models;
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

        HashSet<string> stimIds = new(StringComparer.OrdinalIgnoreCase);

        for (int stimIndex = 0; stimIndex < config.Stims.Count; stimIndex++)
        {
            StimDefinition? stim = config.Stims[stimIndex];
            if (stim is null)
            {
                errors.Add($"stims[{stimIndex}] must be an object.");
                continue;
            }

            ValidateStim(stim, stimIndex, stimIds, errors);
        }

        return errors;
    }

    private static void ValidateStim(StimDefinition stim, int stimIndex, HashSet<string> stimIds, List<string> errors)
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

    private static void ValidateTags(StimDefinition stim, string label, List<string> errors)
    {
        if (stim.Tags is null)
        {
            errors.Add($"{label}: field 'tags' must be an array when provided.");
            return;
        }

        if (stim.Tags.Count > 8)
        {
            errors.Add($"{label}: field 'tags' may contain at most 8 tags.");
        }

        HashSet<string> uniqueTags = new(StringComparer.OrdinalIgnoreCase);
        for (int tagIndex = 0; tagIndex < stim.Tags.Count; tagIndex++)
        {
            string? tag = stim.Tags[tagIndex];
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

    private static void ValidateBuffs(StimDefinition stim, string label, List<string> errors)
    {
        if (stim.Buffs is null)
        {
            errors.Add($"{label}: field 'buffs' is required.");
            return;
        }

        for (int buffIndex = 0; buffIndex < stim.Buffs.Count; buffIndex++)
        {
            BuffDefinition? buff = stim.Buffs[buffIndex];
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

            if (buff.Delay < 0)
            {
                errors.Add($"{buffLabel}.delay must be zero or greater.");
            }
        }
    }

    private static void ValidateTrader(StimDefinition stim, string label, List<string> errors)
    {
        if (stim.Trader?.Enabled != true)
        {
            return;
        }

        TraderDefinition trader = stim.Trader!;

        if (string.IsNullOrWhiteSpace(trader.Trader))
        {
            errors.Add($"{label}: field 'trader.trader' is required when trader.enabled is true.");
        }
        else if (!TraderMappings.IsSupported(trader.Trader))
        {
            errors.Add($"{label}: field 'trader.trader' value '{trader.Trader}' is not supported.");
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
