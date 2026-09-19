namespace MoarSupplies.Models;

/// <summary>
/// Global settings kept separately from individual stim definitions.
/// </summary>
public sealed class ModSettings
{
    public int Version { get; set; } = 1;
    public bool Debug { get; set; }
}
