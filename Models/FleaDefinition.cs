namespace MoarSupplies.Models;

/// <summary>
/// Optional flea-market registration settings. The initial flea price is the
/// configured trader price multiplied by <see cref="PriceMultiplier"/>.
/// </summary>
public sealed class FleaDefinition
{
    public bool Enabled { get; set; }
    /// <summary>Optional flea price before the multiplier. When omitted, an enabled trader price is used.</summary>
    public int? BasePrice { get; set; }
    public double PriceMultiplier { get; set; } = 1;
}
