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

    private static readonly Dictionary<string, BaseItemMapping> SupportedDrinks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aquamari"] = new("5c0fa877d174af02a012e1cf"),
        ["apple-juice"] = new("57513f07245977207e26a311"),
        ["emergency-water-ration"] = new("60098b1705871270cd5352a1"),
        ["grand-juice"] = new("57513f9324597720a7128161"),
        ["hot-rod"] = new("5751496424597720a27126da"),
        ["ice-green-tea"] = new("575062b524597720a31c09a1"),
        ["kvass"] = new("5e8f3423fd7471236e6e3b64"),
        ["max-energy"] = new("5751435d24597720a27126d1"),
        ["milk"] = new("575146b724597720a27126d5"),
        ["moonshine"] = new("5d1b376e86f774252519444e"),
        ["pevko"] = new("62a09f32621468534a797acb"),
        ["pineapple-juice"] = new("544fb62a4bdc2dfb738b4568"),
        ["purified-water"] = new("5d1b33a686f7742523398398"),
        ["ratcola"] = new("60b0f93284c20f0feb453da7"),
        ["tarcola"] = new("57514643245977207f2c2d09"),
        ["tarkovskaya-vodka"] = new("5d40407c86f774318526545a"),
        ["vita-juice"] = new("57513fcc24597720a31c09a6"),
        ["water"] = new("5448fee04bdc2dbc018b4567"),
        ["whiskey"] = new("5d403f9186f7743cac3f229b")
    };

    public static bool IsSupportedDrink(string baseItem) => SupportedDrinks.ContainsKey(baseItem);

    public static bool TryGetDrink(string baseItem, out BaseItemMapping mapping) =>
        SupportedDrinks.TryGetValue(baseItem, out mapping!);

    private static readonly Dictionary<string, BaseItemMapping> SupportedMedicalPacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["afak"] = new("60098ad7c2240c0fe85c570a"),
        ["ai2"] = new("5755356824597772cb798962"),
        ["alu-splint"] = new("5af0454c86f7746bf20992e8"),
        ["analgin"] = new("544fb37f4bdc2dee738b4567"),
        ["army-bandage"] = new("5751a25924597722c463c472"),
        ["augmentin"] = new("590c695186f7741e566b64a2"),
        ["bandage"] = new("544fb25a4bdc2dfb738b4567"),
        ["car"] = new("590c678286f77426c9660122"),
        ["cat"] = new("60098af40accd37ef2175f27"),
        ["ifak"] = new("590c661e86f7741e566b646a"),
        ["calok-b"] = new("5e8488fa988a8701445df1e4"),
        ["esmarch"] = new("5e831507ea0a7c419c2f9bd9"),
        ["golden-star"] = new("5751a89d24597722aa0e8db0"),
        ["grizzly"] = new("590c657e86f77412b013051d"),
        ["ibuprofen"] = new("5af0548586f7743a532b7e99"),
        ["salewa"] = new("544fb45d4bdc2dee738b4568"),
        ["sanitar-afak"] = new("5e99711486f7744bfc4af328"),
        ["sanitar-surgery-kit"] = new("5e99735686f7744bfc4af32c"),
        ["splint"] = new("544fb3364bdc2d34748b456a"),
        ["surv12"] = new("5d02797c86f774203f38e30a"),
        ["vaseline"] = new("5755383e24597772cb798966"),
        ["cms"] = new("5d02778e86f774203e7dedbe"),
    };

    public static bool IsSupportedMedicalPack(string baseItem) => SupportedMedicalPacks.ContainsKey(baseItem);

    public static bool TryGetMedicalPack(string baseItem, out BaseItemMapping mapping) =>
        SupportedMedicalPacks.TryGetValue(baseItem, out mapping!);
}

/// <summary>
/// The SPT template identity of a supported friendly base-item name.
/// </summary>
public sealed record BaseItemMapping(string TemplateId);
