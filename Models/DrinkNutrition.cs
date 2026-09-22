namespace MoarSupplies.Models;

/// <summary>
/// Immediate nutrition changes applied when a drink is consumed.
/// </summary>
public sealed class DrinkNutrition
{
    public double Hydration { get; set; }
    public double Energy { get; set; }
}
