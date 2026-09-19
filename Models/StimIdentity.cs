namespace MoarSupplies.Models;

/// <summary>
/// Display text shown to users for a custom stimulant.
/// </summary>
public sealed class StimIdentity
{
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
