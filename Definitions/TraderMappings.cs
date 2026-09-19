namespace MoarSupplies.Definitions;

/// <summary>
/// Friendly trader names supported by the initial configuration schema.
/// SPT trader IDs remain internal and are never exposed in stims.json.
/// </summary>
public static class TraderMappings
{
    private static readonly Dictionary<string, TraderMapping> SupportedTraders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["therapist"] = new("54cb57776803fa99248b456e"),
        ["prapor"] = new("54cb50c76803fa8b248b4571"),
        ["skier"] = new("58330581ace78e27b8b10cee")
    };

    public static bool IsSupported(string trader) => SupportedTraders.ContainsKey(trader);

    public static bool TryGet(string trader, out TraderMapping mapping) =>
        SupportedTraders.TryGetValue(trader, out mapping!);
}

public sealed record TraderMapping(string TraderId);
