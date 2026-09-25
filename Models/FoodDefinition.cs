namespace MoarSupplies.Models;

/// <summary>
/// A friendly, user-configured food definition. Food keeps its vanilla eating
/// behavior while allowing resource and immediate nutrition to be configured.
/// </summary>
public sealed class FoodDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = string.Empty;
    public int Resource { get; set; }
    public DrinkNutrition Nutrition { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public TraderDefinition? Trader { get; set; } = new();
}
