namespace MoarSupplies.Models;

using System.Text.Json.Serialization;

/// <summary>
/// A configurable medical kit clone. The cloned item's native treatment effects
/// are retained; resource, per-use healing, surgery restoration, and use time
/// can be replaced when the selected item supports them.
/// </summary>
public sealed class MedicalPackDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public StimIdentity Identity { get; set; } = new();
    public string BaseItem { get; set; } = string.Empty;
    /// <summary>
    /// Native medical resource. This is a total HP pool for med kits, a use count
    /// for surgery kits, and may be zero for one-use treatment items.
    /// </summary>
    public int Resource { get; set; }
    public int ResourceRate { get; set; }
    /// <summary>Resource consumed when treating light bleeding. Null keeps the vanilla value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LightBleedingCost { get; set; }
    /// <summary>Resource consumed when treating heavy bleeding. Null keeps the vanilla value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? HeavyBleedingCost { get; set; }
    /// <summary>Resource consumed when treating radiation exposure. Null keeps the vanilla value.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RadiationTreatmentCost { get; set; }
    public double? UseTimeMultiplier { get; set; }
    public double? SurgeryRestoreMultiplier { get; set; }
    public List<string> Tags { get; set; } = [];
    public TraderDefinition? Trader { get; set; }
}
