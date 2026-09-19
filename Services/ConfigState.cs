using MoarSupplies.Models;
using SPTarkov.DI.Annotations;

namespace MoarSupplies.Services;

/// <summary>
/// Provides the successfully loaded user configuration to the web panel.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class ConfigState
{
    public ModConfig? Current { get; set; }
}
