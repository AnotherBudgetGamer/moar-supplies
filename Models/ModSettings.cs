namespace MoarSupplies.Models;

/// <summary>
/// Global settings kept separately from individual stim definitions.
/// </summary>
public sealed class ModSettings
{
    public int Version { get; set; } = 1;
    public bool Debug { get; set; }
    /// <summary>
    /// Creates a diagnostic clone for every mapped vanilla base item that is not
    /// already used by a configured supply definition.
    /// </summary>
    public bool EnableMappedItemTestClones { get; set; }
}
