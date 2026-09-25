namespace MoarSupplies.Definitions;

/// <summary>
/// Friendly effects supported by the initial configuration schema.
/// SPT implementation values remain internal and are never exposed in stims.json.
/// </summary>
public static class BuffMappings
{
    private static readonly Dictionary<string, BuffMapping> SupportedBuffs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["antidote"] = new("Antidote", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysBeneficial),
        ["aiming"] = new("SkillRate", "AimDrills"),
        ["assault"] = new("SkillRate", "Assault"),
        ["attachedLauncher"] = new("SkillRate", "AttachedLauncher"),
        ["attention"] = new("SkillRate", "Attention"),
        ["bodyTemperature"] = new("BodyTemperature"),
        ["charisma"] = new("SkillRate", "Charisma"),
        ["concussion"] = new("Contusion", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["covertMovement"] = new("SkillRate", "CovertMovement"),
        ["crafting"] = new("SkillRate", "Crafting"),
        ["damageModifier"] = new("DamageModifier", AbsoluteValue: false, Polarity: EffectPolarity.InvertedValue),
        ["dmr"] = new("SkillRate", "DMR"),
        ["endurance"] = new("SkillRate", "Endurance"),
        ["immunity"] = new("SkillRate", "Immunity"),
        ["fieldMedicine"] = new("SkillRate", "FieldMedicine"),
        ["firstAid"] = new("SkillRate", "FirstAid"),
        ["fracture"] = new("Fracture", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["frostbite"] = new("FrostbiteBuff", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["heavyBleeding"] = new("HeavyBleeding", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["heavyVests"] = new("SkillRate", "HeavyVests"),
        ["hideoutManagement"] = new("SkillRate", "HideoutManagement"),
        ["hmg"] = new("SkillRate", "HMG"),
        ["intellect"] = new("SkillRate", "Intellect"),
        ["launcher"] = new("SkillRate", "Launcher"),
        ["lightBleeding"] = new("LightBleeding", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["lightVests"] = new("SkillRate", "LightVests"),
        ["lmg"] = new("SkillRate", "LMG"),
        ["lockpicking"] = new("SkillRate", "Lockpicking"),
        ["magDrills"] = new("SkillRate", "MagDrills"),
        ["meleeDamage"] = new("SkillRate", "Melee", AbsoluteValue: false),
        ["memory"] = new("SkillRate", "Memory"),
        ["metabolism"] = new("SkillRate", "Metabolism"),
        ["maxStamina"] = new("MaxStamina"),
        ["nightOps"] = new("SkillRate", "NightOps"),
        // Pain is the harmful stimulant buff. Pain suppression is instead a
        // direct medical-item effect (the same path used by vanilla morphine).
        ["pain"] = new("Pain", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["painSuppression"] = new(ItemEffectType: ItemEffectType.PainSuppression, RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysBeneficial),
        ["perception"] = new("SkillRate", "Perception"),
        ["pistol"] = new("SkillRate", "Pistol"),
        ["proneMovement"] = new("SkillRate", "ProneMovement"),
        ["recoilControl"] = new("SkillRate", "RecoilControl", AbsoluteValue: false),
        ["removeAllBloodLosses"] = new("RemoveAllBloodLosses", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysBeneficial),
        ["revolver"] = new("SkillRate", "Revolver"),
        ["search"] = new("SkillRate", "Search"),
        ["shotgun"] = new("SkillRate", "Shotgun"),
        ["silentOps"] = new("SkillRate", "SilentOps"),
        ["smg"] = new("SkillRate", "SMG"),
        ["sniper"] = new("SkillRate", "Sniper"),
        ["sniping"] = new("SkillRate", "Sniping"),
        ["sprintInertia"] = new("SkillRate", "Strength", AbsoluteValue: false),
        ["staminaRate"] = new("StaminaRate"),
        ["strength"] = new("SkillRate", "Strength"),
        ["stressResistance"] = new("SkillRate", "StressResistance"),
        ["surgery"] = new("SkillRate", "Surgery"),
        ["energyRate"] = new("EnergyRate"),
        ["handsTremor"] = new("HandsTremor", AbsoluteValue: false, Polarity: EffectPolarity.InvertedValue),
        ["health"] = new("SkillRate", "Health"),
        ["healthRate"] = new("HealthRate"),
        ["hearingDistance"] = new("SkillRate", "Perception", AbsoluteValue: false),
        ["hydrationRate"] = new("HydrationRate"),
        ["skillHealth"] = new("SkillRate", "Health"),
        ["tunnelVision"] = new("QuantumTunnelling", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["throwingStrength"] = new("SkillRate", "Throwing"),
        ["troubleshooting"] = new("SkillRate", "TroubleShooting"),
        ["unknownToxin"] = new("UnknownToxin", RequiresValue: false, AbsoluteValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["vitality"] = new("SkillRate", "Vitality"),
        ["weaponErgonomics"] = new("SkillRate", "WeaponModding"),
        ["weaponSpread"] = new("WeaponSpread", AbsoluteValue: false, Polarity: EffectPolarity.InvertedValue),
        ["weaponSwapSpeed"] = new("SkillRate", "DrawMaster", AbsoluteValue: false),
        ["weaponTreatment"] = new("SkillRate", "WeaponTreatment"),
        ["weightLimit"] = new("WeightLimit", AbsoluteValue: false),
        ["quantumTunnelling"] = new("QuantumTunnelling", RequiresValue: false, Polarity: EffectPolarity.AlwaysHarmful),
        ["zombieInfection"] = new("ZombieInfection", Polarity: EffectPolarity.AlwaysHarmful)
    };

    public static bool TryGet(string effect, out BuffMapping mapping) => SupportedBuffs.TryGetValue(effect, out mapping!);

    public static bool IsStimulatorBuff(string effect) =>
        TryGet(effect, out BuffMapping mapping) && mapping.ItemEffectType == ItemEffectType.None;

    /// <summary>
    /// Identifies whether an effect is presented as a drawback in the mod web UI.
    /// </summary>
    public static bool IsDebuff(string effect, double? value)
    {
        if (!TryGet(effect, out BuffMapping mapping))
        {
            return false;
        }

        return mapping.Polarity switch
        {
            EffectPolarity.AlwaysHarmful => true,
            EffectPolarity.AlwaysBeneficial => false,
            EffectPolarity.InvertedValue => value > 0,
            _ => value < 0
        };
    }
}

/// <summary>
/// Describes how a friendly effect is represented by SPT.
/// </summary>
public sealed record BuffMapping(
    string BuffType = "",
    string? SkillName = null,
    bool RequiresValue = true,
    bool AbsoluteValue = true,
    EffectPolarity Polarity = EffectPolarity.ByValue,
    ItemEffectType ItemEffectType = ItemEffectType.None);

/// <summary>Effects applied by medical-item data instead of stimulant buffs.</summary>
public enum ItemEffectType
{
    None,
    PainSuppression
}

/// <summary>
/// Defines how an effect is visually classified when displayed to users.
/// </summary>
public enum EffectPolarity
{
    ByValue,
    InvertedValue,
    AlwaysBeneficial,
    AlwaysHarmful
}
