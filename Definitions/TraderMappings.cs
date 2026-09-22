namespace MoarSupplies.Definitions;

/// <summary>
/// Friendly vanilla trader names supported by the configuration schema.
/// SPT trader IDs remain internal and are never exposed in stims.json.
/// </summary>
public static class TraderMappings
{
    private static readonly Dictionary<string, TraderMapping> SupportedTraders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["therapist"] = new("54cb57776803fa99248b456e"),
        ["prapor"] = new("54cb50c76803fa8b248b4571"),
        ["skier"] = new("58330581ace78e27b8b10cee"),
        ["peacekeeper"] = new("5935c25fb3acc3127c3d8cd9"),
        ["mechanic"] = new("5a7c2eca46aef81a7ca2145d"),
        ["ragman"] = new("5ac3b934156ae10c4430e83c"),
        ["jaeger"] = new("5c0647fdd443bc2504c2d371"),
        ["fence"] = new("579dc571d53a0658a154fbec")
    };

    public static bool IsSupported(string trader) => SupportedTraders.ContainsKey(trader);

    public static bool TryGet(string trader, out TraderMapping mapping) =>
        SupportedTraders.TryGetValue(trader, out mapping!);
}

public sealed record TraderMapping(string TraderId);
