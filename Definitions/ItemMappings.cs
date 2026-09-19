namespace MoarSupplies.Definitions;

/// <summary>
/// Friendly names for every stimulator template shipped by the supported SPT version.
/// Template IDs remain internal implementation details and are never exposed in stims.json.
/// </summary>
public static class ItemMappings
{
    private static readonly Dictionary<string, BaseItemMapping> SupportedItems = new(StringComparer.OrdinalIgnoreCase)
    {
        ["2a2btg"] = new("66507eabf5ddb0818b085b68"),
        ["3btg"] = new("5ed515c8d380ab312177c0fa"),
        ["adrenaline"] = new("5c10c8fd86f7743d7d706df3"),
        ["ahf1m"] = new("5ed515f6915ec335206e4152"),
        ["etgchange"] = new("5c0e534186f7747fa1419867"),
        ["l1"] = new("5ed515e03a40a50460332579"),
        ["meldonin"] = new("5ed5160a87bb8443d10680b5"),
        ["pnb"] = new("637b6179104668754b72f8f5"),
        ["mule"] = new("5ed51652f6c34d2cc26336a1"),
        ["obdolbos"] = new("5ed5166ad380ab312177c100"),
        ["obdolbos2"] = new("637b60c3b7afa97bfc3d7001"),
        ["p22"] = new("5ed515ece452db0eb56fc028"),
        ["perfotoran"] = new("637b6251104668754b72f8f9"),
        ["propital"] = new("5c0e530286f7747fa1419862"),
        ["sj1"] = new("5c0e531286f7747fa54205c2"),
        ["sj6"] = new("5c0e531d86f7747fa23f4d42"),
        ["sj9"] = new("5fca13ca637ee0341a484f46"),
        ["sj12"] = new("637b612fb7afa97bfc3d7005"),
        ["trimadol"] = new("637b620db7afa97bfc3d7009"),
        ["xtg12"] = new("5fca138c2a7b221b2852a5c6"),
        ["zagustin"] = new("5c0e533786f7747fa23f4d47"),

        // Kept for existing configs. Morphine is not a Stimulator-template item.
        ["morphine"] = new("544fb3f34bdc2d03748b456a")
    };

    public static bool IsSupported(string baseItem) => SupportedItems.ContainsKey(baseItem);

    public static bool TryGet(string baseItem, out BaseItemMapping mapping) =>
        SupportedItems.TryGetValue(baseItem, out mapping!);
}

/// <summary>
/// The SPT template identity of a supported friendly base-item name.
/// </summary>
public sealed record BaseItemMapping(string TemplateId);
