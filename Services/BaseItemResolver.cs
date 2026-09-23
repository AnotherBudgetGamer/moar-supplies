using MoarSupplies.Definitions;
using SPTarkov.DI.Annotations;

namespace MoarSupplies.Services;

/// <summary>
/// Resolves user-facing base-item names to their internal SPT template identities.
/// Item cloning and database access are intentionally deferred to later milestones.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class BaseItemResolver
{
    public bool TryResolve(string baseItem, out BaseItemMapping mapping) =>
        ItemMappings.TryGet(baseItem, out mapping);

    public bool TryResolveDrink(string baseItem, out BaseItemMapping mapping) =>
        ItemMappings.TryGetDrink(baseItem, out mapping);

    public bool TryResolveMedicalPack(string baseItem, out BaseItemMapping mapping) =>
        ItemMappings.TryGetMedicalPack(baseItem, out mapping);
}
