namespace MoarSupplies.Models;

/// <summary>
/// Optional user-facing trader sale settings for a custom stimulant.
/// </summary>
public sealed class TraderDefinition
{
    public bool Enabled { get; set; }
    /// <summary>
    /// Friendly trader name shown in the workshop and retained in configuration files.
    /// </summary>
    public string Trader { get; set; } = string.Empty;
    /// <summary>
    /// Stable SPT trader ID selected by the workshop. Optional for legacy vanilla definitions.
    /// </summary>
    public string? TraderId { get; set; }
    public int LoyaltyLevel { get; set; }
    public int Price { get; set; }
}
