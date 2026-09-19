using SPTarkov.DI.Annotations;

namespace MoarSupplies.Services;

/// <summary>
/// Holds the current configuration's optional verbose logging setting.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class DebugSettings
{
    public bool Enabled { get; set; }
}
