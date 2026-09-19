namespace MoarSupplies.Models;

/// <summary>
/// A friendly, user-configured stimulant effect.
/// </summary>
public sealed class BuffDefinition
{
    public string Effect { get; set; } = string.Empty;
    public double? Value { get; set; }
    public int Duration { get; set; }
    public int Delay { get; set; }
}
