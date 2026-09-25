namespace MoarSupplies.Models;

/// <summary>
/// Root user-facing configuration document for Moar Supplies.
/// </summary>
public sealed class ModConfig
{
    public int Version { get; set; }
    public bool Debug { get; set; }
    public List<StimDefinition> Stims { get; set; } = [];
    public List<DrinkDefinition> Drinks { get; set; } = [];
    public List<FoodDefinition> Foods { get; set; } = [];
    public List<MedicalPackDefinition> MedicalPacks { get; set; } = [];
}
