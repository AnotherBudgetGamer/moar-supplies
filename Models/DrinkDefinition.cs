namespace MoarSupplies.Models;

/// <summary>
/// A friendly, user-configured drink definition. Drink resource and nutrition are
/// deliberately separate from stim uses and timed effects.
/// </summary>
public sealed class DrinkDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = string.Empty;
    public int Resource { get; set; }
    public DrinkNutrition Nutrition { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public List<BuffDefinition> Buffs { get; set; } = [];
    public TraderDefinition? Trader { get; set; }
}
