namespace MoarSupplies.Models;

/// <summary>
/// A single friendly stimulant definition. It contains no SPT implementation types or IDs.
/// </summary>
public sealed class StimDefinition : ICraftableDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = string.Empty;
    public int Uses { get; set; }
    public List<string> Tags { get; set; } = [];
    /// <summary>Relative world-loot weight. One equals the vanilla base item's weight.</summary>
    public double WorldLootWeight { get; set; } = 1;
    public List<BuffDefinition> Buffs { get; set; } = [];
    public TraderDefinition? Trader { get; set; }
    public CraftDefinition? Craft { get; set; }
}
