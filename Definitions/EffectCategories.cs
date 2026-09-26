namespace MoarSupplies.Definitions;

/// <summary>
/// Shared, player-facing groups for selecting effects in the web editor.
/// Keep these aligned with the effect toolkit shown on the dashboard.
/// </summary>
public static class EffectCategories
{
    private static readonly EffectCategory[] Categories =
    [
        new("Combat & weapons", "combat", ["aiming", "assault", "attachedLauncher", "damageModifier", "dmr", "hmg", "launcher", "lmg", "magDrills", "meleeDamage", "pistol", "recoilControl", "revolver", "shotgun", "smg", "sniper", "sniping", "throwingStrength", "weaponErgonomics", "weaponSpread", "weaponSwapSpeed", "weaponTreatment"]),
        new("Stamina & movement", "stamina", ["covertMovement", "endurance", "heavyVests", "lightVests", "maxStamina", "proneMovement", "sprintInertia", "staminaRate", "strength", "weightLimit"]),
        new("Health & survival", "health", ["antidote", "bodyTemperature", "energyRate", "fieldMedicine", "firstAid", "health", "healthRate", "hydrationRate", "immunity", "metabolism", "pain", "painSuppression", "removeAllBloodLosses", "skillHealth", "surgery", "vitality"]),
        new("Mind & skills", "mind", ["attention", "charisma", "crafting", "hearingDistance", "hideoutManagement", "intellect", "lockpicking", "memory", "nightOps", "perception", "search", "silentOps", "stressResistance", "troubleshooting"]),
        new("Risk & tradeoffs", "risk", ["concussion", "fracture", "frostbite", "handsTremor", "heavyBleeding", "lightBleeding", "quantumTunnelling", "tunnelVision", "unknownToxin", "zombieInfection"])
    ];

    /// <summary>Returns only categories containing effects available to the current item type.</summary>
    public static IReadOnlyList<EffectCategory> For(IEnumerable<string> availableEffects)
    {
        HashSet<string> available = new(availableEffects, StringComparer.OrdinalIgnoreCase);
        List<EffectCategory> groups = [];

        foreach (EffectCategory category in Categories)
        {
            string[] effects = category.Effects.Where(available.Contains).ToArray();
            if (effects.Length > 0)
            {
                groups.Add(category with { Effects = effects });
                available.ExceptWith(effects);
            }
        }

        if (available.Count > 0)
        {
            groups.Add(new EffectCategory("Other effects", "other", available.OrderBy(effect => effect, StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        return groups;
    }
}

public sealed record EffectCategory(string Name, string Color, IReadOnlyList<string> Effects);
