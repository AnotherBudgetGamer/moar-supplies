namespace MoarSupplies.Models;

/// <summary>A searchable native SPT item exposed by the crafting editor.</summary>
public sealed record CraftIngredientOption(string TemplateId, string Name, string ShortName)
{
    public string DisplayName => string.IsNullOrWhiteSpace(ShortName) || string.Equals(Name, ShortName, StringComparison.OrdinalIgnoreCase)
        ? Name
        : $"{Name} ({ShortName})";
}
