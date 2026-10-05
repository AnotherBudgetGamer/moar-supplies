namespace MoarSupplies.Models;

/// <summary>
/// A friendly, user-configured drink definition. Drink resource and nutrition are
/// deliberately separate from stim uses and timed effects.
/// </summary>
public sealed class DrinkDefinition : ICraftableDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = string.Empty;
    public int Resource { get; set; }
    public DrinkNutrition Nutrition { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    /// <summary>Relative world-loot weight. One equals the vanilla base item's weight.</summary>
    public double WorldLootWeight { get; set; } = 1;
    public List<BuffDefinition> Buffs { get; set; } = [];
    public TraderDefinition? Trader { get; set; }
    public CraftDefinition? Craft { get; set; }
}
