namespace MoarSupplies.Definitions;

/// <summary>
/// Player-facing guidance shown by the stimulant editor. These descriptions use
/// friendly effect names so authors do not need to understand SPT buff IDs.
/// </summary>
public static class EffectHelp
{
    private static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["antidote"] = "Cures toxin effects. This effect has no value; it applies for the selected duration.",
        ["bodyTemperature"] = "Adjusts body temperature. Positive values raise it and negative values lower it.",
        ["concussion"] = "Applies a concussion. This effect has no value; duration controls how long the condition lasts.",
        ["damageModifier"] = "Changes incoming damage to every body part except the head. Positive values increase damage taken; negative values reduce it.",
        ["energyRate"] = "Changes energy recovery over time. Positive values restore energy faster; negative values drain it.",
        ["fracture"] = "Applies a fracture. This effect has no value; duration controls how long the condition lasts.",
        ["frostbite"] = "Applies frostbite. This effect has no value; duration controls how long the condition lasts.",
        ["handsTremor"] = "Changes hand tremor. Positive values add more shake; negative values reduce it.",
        ["healthRate"] = "Changes health recovery over time. Positive values heal; negative values damage over time.",
        ["heavyBleeding"] = "Applies heavy bleeding. This effect has no value; duration controls how long the condition lasts.",
        ["hearingDistance"] = "Changes how far sounds can be heard. Positive values extend hearing distance; negative values shorten it.",
        ["hydrationRate"] = "Changes hydration recovery over time. Positive values restore hydration; negative values dehydrate.",
        ["maxStamina"] = "Changes the maximum stamina pool. Positive values add stamina; negative values remove it.",
        ["lightBleeding"] = "Applies light bleeding. This effect has no value; duration controls how long the condition lasts.",
        ["meleeDamage"] = "Changes melee damage. Positive values increase damage; negative values reduce it.",
        ["pain"] = "Applies pain. This effect has no value; duration controls how long pain lasts.",
        ["painSuppression"] = "Suppresses pain. This effect has no value; duration controls how long pain is suppressed.",
        ["recoilControl"] = "Changes recoil control. Positive values make recoil harder to control; negative values make it easier.",
        ["removeAllBloodLosses"] = "Removes all bleeding effects. This effect has no value; it is applied when the stimulant takes effect.",
        ["sprintInertia"] = "Changes sprint inertia. Positive values make sprint movement more responsive; negative values make it more sluggish.",
        ["staminaRate"] = "Changes stamina recovery over time. Positive values recover stamina faster; negative values slow recovery or drain it.",
        ["tunnelVision"] = "Applies tunnel vision. This effect has no value; duration controls how long the visual impairment lasts.",
        ["unknownToxin"] = "Applies an unknown toxin. This effect has no value; duration controls how long the condition lasts.",
        ["weaponSpread"] = "Changes weapon spread. Positive values widen spread; negative values tighten it.",
        ["weaponSwapSpeed"] = "Changes weapon swap speed. Positive values swap weapons faster; negative values slow swapping.",
        ["weightLimit"] = "Changes carrying capacity as a multiplier. Use 0.5 for +50%, 1 for +100%, or -0.5 for -50% carrying capacity.",
        ["quantumTunnelling"] = "Applies the quantum tunnelling condition. This effect has no value; duration controls how long the condition lasts.",
        ["zombieInfection"] = "Applies zombie infection. Positive values increase the infection effect; use a small value and test it in-game."
    };

    public static string Describe(string effect)
    {
        if (Descriptions.TryGetValue(effect, out string? description)) return description;
        if (BuffMappings.TryGet(effect, out BuffMapping mapping) && mapping.BuffType == "SkillRate")
            return $"Changes the rate at which the {DisplaySkillName(mapping.SkillName)} skill improves while active. Positive values increase the rate; negative values reduce it.";
        return "Changes this effect for the selected duration. Positive values increase it; negative values reduce it.";
    }

    private static string DisplaySkillName(string? skillName) => skillName switch
    {
        "AimDrills" => "aiming", "AttachedLauncher" => "attached launcher", "CovertMovement" => "covert movement",
        "DMR" => "designated marksman rifle", "FirstAid" => "first aid", "HeavyVests" => "heavy vest",
        "HideoutManagement" => "hideout management", "HMG" => "heavy machine gun", "LightVests" => "light vest",
        "Lockpicking" => "lockpicking", "MagDrills" => "magazine drills", "NightOps" => "night operations",
        "ProneMovement" => "prone movement", "RecoilControl" => "recoil control", "SilentOps" => "silent operations",
        "SMG" => "submachine gun", "TroubleShooting" => "troubleshooting", "WeaponModding" => "weapon modding",
        "WeaponTreatment" => "weapon treatment", "DrawMaster" => "weapon draw", "Melee" => "melee", "Throwing" => "throwing",
        null or "" => "affected",
        _ => System.Text.RegularExpressions.Regex.Replace(skillName, "(?<!^)([A-Z])", " $1").ToLowerInvariant()
    };
}
