namespace MoarSupplies.Models;

/// <summary>
/// A hideout recipe that produces one configured Moar Supplies item.
/// Ingredient template IDs are native SPT template IDs and deliberately remain
/// separate from the friendly clone-source mappings.
/// </summary>
public sealed class CraftDefinition
{
    public bool Enabled { get; set; } = true;
    public string Bench { get; set; } = "medStation";
    public int BenchLevel { get; set; } = 1;
    public int DurationSeconds { get; set; } = 60;
    public int OutputCount { get; set; } = 1;
    public bool RequiresFuel { get; set; }
    public List<CraftIngredientDefinition> Ingredients { get; set; } = [];
}

/// <summary>A consumed item required by a hideout craft.</summary>
public sealed class CraftIngredientDefinition
{
    public string TemplateId { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
}

/// <summary>Implemented by every Moar Supplies definition that can be crafted.</summary>
public interface ICraftableDefinition
{
    string Id { get; }
    CraftDefinition? Craft { get; }
}
