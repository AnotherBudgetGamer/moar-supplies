namespace MoarSupplies.Models;

/// <summary>
/// A friendly, user-configured food definition. Food keeps its vanilla eating
/// behavior while allowing resource and immediate nutrition to be configured.
/// </summary>
public sealed class FoodDefinition : ICraftableDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = string.Empty;
    public int Resource { get; set; }
    public DrinkNutrition Nutrition { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    /// <summary>Relative world-loot weight. One equals the vanilla base item's weight.</summary>
    public double WorldLootWeight { get; set; } = 1;
    public TraderDefinition? Trader { get; set; } = new();
    public FleaDefinition Flea { get; set; } = new();
    public CraftDefinition? Craft { get; set; }
}
