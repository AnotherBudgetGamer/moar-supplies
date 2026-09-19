namespace MoarSupplies.Models;

/// <summary>
/// Optional user-facing trader sale settings for a custom stimulant.
/// </summary>
public sealed class TraderDefinition
{
    public bool Enabled { get; set; }
    public string Trader { get; set; } = string.Empty;
    public int LoyaltyLevel { get; set; }
    public int Price { get; set; }
}
