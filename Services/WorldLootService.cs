using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using System.Security.Cryptography;
using System.Text;

namespace MoarSupplies.Services;

/// <summary>
/// Adds custom supplies to the same static-container and dynamic loose-loot pools
/// as their vanilla base items. Each matching pool keeps its original total
/// weight: the vanilla item has a weight of one and each clone uses its
/// configured relative weight.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class WorldLootService
{
    private readonly ILogger<WorldLootService> _logger;
    private readonly DebugSettings _debugSettings;
    private readonly LocationTable _locationTable;

    public WorldLootService(
        ILogger<WorldLootService> logger,
        DebugSettings debugSettings,
        LocationTable locationTable)
    {
        _logger = logger;
        _debugSettings = debugSettings;
        _locationTable = locationTable;
    }

    /// <summary>
    /// Registers every clone after the item templates exist. Clones inherit the
    /// exact container and loose-loot coverage of their base item, rather than
    /// being added to a broad generic pool that could put medical supplies in
    /// unsuitable places.
    /// </summary>
    public int Register(IEnumerable<WorldLootInjection> injections)
    {
        Dictionary<string, WorldLootInjection[]> clonesByBase = injections
            .GroupBy(injection => injection.BaseTemplateId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Where(injection => injection.Weight > 0)
                    .GroupBy(injection => injection.CloneTemplateId, StringComparer.OrdinalIgnoreCase)
                    .Select(clones => clones.First())
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);

        clonesByBase = clonesByBase
            .Where(entry => entry.Value.Length > 0)
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
        if (clonesByBase.Count == 0) return 0;

        int updatedPoolCount = 0;
        int addedItemCount = 0;
        int updatedMapCount = 0;
        int looseLootMapCount = 0;

        foreach (KeyValuePair<string, Location> location in _locationTable.GetDictionary())
        {
            Dictionary<SPTarkov.Server.Core.Models.Common.MongoId, StaticLootDetails>? staticLoot = location.Value.StaticLoot?.Value;
            int locationPoolCount = 0;
            if (staticLoot is not null)
            {
                foreach (StaticLootDetails containerLoot in staticLoot.Values)
                {
                    if (containerLoot.ItemDistribution is null) continue;

                    List<ItemDistribution> distribution = containerLoot.ItemDistribution.ToList();
                    bool updated = false;

                    foreach (KeyValuePair<string, WorldLootInjection[]> entry in clonesByBase)
                    {
                        ItemDistribution? baseItem = distribution.FirstOrDefault(item =>
                            string.Equals(item.Tpl.ToString(), entry.Key, StringComparison.OrdinalIgnoreCase));
                        if (baseItem is null || baseItem.RelativeProbability is not float probability || probability <= 0) continue;

                        double totalWeight = 1 + entry.Value.Sum(injection => injection.Weight);
                        baseItem.RelativeProbability = (float)(probability / totalWeight);
                        foreach (WorldLootInjection injection in entry.Value)
                        {
                            distribution.Add(new ItemDistribution
                            {
                                Tpl = injection.CloneTemplateId,
                                RelativeProbability = (float)(probability * injection.Weight / totalWeight)
                            });
                            addedItemCount++;
                        }

                        updated = true;
                    }

                    if (!updated) continue;

                    containerLoot.ItemDistribution = distribution;
                    updatedPoolCount++;
                    locationPoolCount++;
                }
            }

            if (location.Value.LooseLoot is { } looseLoot)
            {
                string locationId = location.Key;
                looseLoot.AddTransformer(loadedLooseLoot =>
                {
                    (int updatedSpawnpoints, int addedEntries) = RegisterLooseLoot(loadedLooseLoot, clonesByBase);
                    if (_debugSettings.Enabled && addedEntries > 0)
                    {
                        _logger.LogInformation(
                            "[MoarSupplies] Added {EntryCount} custom supply entry(s) to {SpawnpointCount} loose-loot spawnpoint pool(s) on {LocationId}.",
                            addedEntries,
                            updatedSpawnpoints,
                            locationId);
                    }

                    return loadedLooseLoot;
                });
                looseLootMapCount++;
            }

            if (locationPoolCount > 0) updatedMapCount++;
        }

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation(
                "[MoarSupplies] Added {CloneCount} custom supply template(s) to {PoolCount} static loot pool(s) across {MapCount} map(s), creating {StaticEntryCount} weighted entries. Loose-loot injection is registered lazily for {LooseMapCount} map(s).",
                clonesByBase.Values.Sum(clones => clones.Length),
                updatedPoolCount,
                updatedMapCount,
                addedItemCount,
                looseLootMapCount);
        }

        return addedItemCount;
    }

    /// <summary>
    /// Dynamic loose loot is represented as complete item compositions, not a
    /// simple list of template IDs. A custom supply therefore needs its own
    /// composition which is identical to the vanilla one except for the cloned
    /// supply template. This keeps the spawn point, position, and any companion
    /// items intact while making the clone selectable by the normal generator.
    /// </summary>
    private static (int UpdatedSpawnpoints, int AddedEntries) RegisterLooseLoot(
        LooseLoot? looseLoot,
        IReadOnlyDictionary<string, WorldLootInjection[]> clonesByBase)
    {
        if (looseLoot?.Spawnpoints is null) return (0, 0);

        int updatedSpawnpoints = 0;
        int addedEntries = 0;
        foreach (Spawnpoint spawnpoint in looseLoot.Spawnpoints)
        {
            if (spawnpoint.Template?.Items is null || spawnpoint.ItemDistribution is null) continue;

            List<SptLootItem> items = spawnpoint.Template.Items.ToList();
            List<LooseLootItemDistribution> distribution = spawnpoint.ItemDistribution.ToList();
            bool updated = false;

            foreach (LooseLootItemDistribution sourceDistribution in spawnpoint.ItemDistribution)
            {
                string? sourceKey = sourceDistribution.ComposedKey?.Key;
                if (string.IsNullOrWhiteSpace(sourceKey) || sourceDistribution.RelativeProbability is not double probability || probability <= 0) continue;

                List<SptLootItem> composition = GetCompositionItems(items, sourceKey);
                if (composition.Count == 0) continue;

                foreach (KeyValuePair<string, WorldLootInjection[]> entry in clonesByBase)
                {
                    if (!composition.Any(item => string.Equals(item.Template.ToString(), entry.Key, StringComparison.OrdinalIgnoreCase))) continue;

                    double totalWeight = 1 + entry.Value.Sum(injection => injection.Weight);
                    sourceDistribution.RelativeProbability = probability / totalWeight;
                    foreach (WorldLootInjection injection in entry.Value)
                    {
                        string cloneKey = CreateLooseCompositionKey(sourceKey, injection.CloneTemplateId);
                        items.AddRange(CloneComposition(composition, sourceKey, cloneKey, entry.Key, injection.CloneTemplateId));
                        distribution.Add(new LooseLootItemDistribution
                        {
                            ComposedKey = new ComposedKey { Key = cloneKey },
                            RelativeProbability = probability * injection.Weight / totalWeight
                        });
                        addedEntries++;
                    }

                    updated = true;
                }
            }

            if (!updated) continue;

            spawnpoint.Template.Items = items;
            spawnpoint.ItemDistribution = distribution;
            updatedSpawnpoints++;
        }

        return (updatedSpawnpoints, addedEntries);
    }

    private static List<SptLootItem> GetCompositionItems(IEnumerable<SptLootItem> items, string sourceKey)
    {
        List<SptLootItem> allItems = items.ToList();
        List<SptLootItem> composition = allItems
            .Where(item => string.Equals(item.ComposedKey, sourceKey, StringComparison.Ordinal))
            .ToList();
        HashSet<string> itemIds = composition.Select(item => item.Id.ToString()).ToHashSet(StringComparer.Ordinal);

        // A composition can be a loose container with child items. Those children
        // do not carry the composed key themselves, so include them by following
        // their parent IDs before creating the alternate composition.
        for (int previousCount = -1; previousCount != composition.Count; previousCount = composition.Count)
        {
            foreach (SptLootItem child in allItems)
            {
                if (child.ParentId is null || !itemIds.Contains(child.ParentId) || !itemIds.Add(child.Id.ToString())) continue;
                composition.Add(child);
            }
        }

        return composition;
    }

    private static IEnumerable<SptLootItem> CloneComposition(
        IEnumerable<SptLootItem> composition,
        string sourceKey,
        string cloneKey,
        string baseTemplateId,
        string cloneTemplateId)
    {
        List<SptLootItem> sourceItems = composition.ToList();
        Dictionary<string, string> itemIdMap = sourceItems.ToDictionary(
            item => item.Id.ToString(),
            item => CreateLooseItemId(cloneKey, item.Id.ToString()),
            StringComparer.Ordinal);

        foreach (SptLootItem source in sourceItems)
        {
            string sourceId = source.Id.ToString();
            yield return new SptLootItem
            {
                ComposedKey = string.Equals(source.ComposedKey, sourceKey, StringComparison.Ordinal) ? cloneKey : source.ComposedKey,
                Id = itemIdMap[sourceId],
                Template = string.Equals(source.Template.ToString(), baseTemplateId, StringComparison.OrdinalIgnoreCase)
                    ? cloneTemplateId
                    : source.Template,
                ParentId = source.ParentId is not null && itemIdMap.TryGetValue(source.ParentId, out string? clonedParentId)
                    ? clonedParentId
                    : source.ParentId,
                SlotId = source.SlotId,
                Location = source.Location,
                Desc = source.Desc,
                Upd = source.Upd
            };
        }
    }

    private static string CreateLooseCompositionKey(string sourceKey, string cloneTemplateId) =>
        "moar-" + CreateHash(sourceKey + ":" + cloneTemplateId, 16);

    private static string CreateLooseItemId(string compositionKey, string sourceItemId) =>
        CreateHash(compositionKey + ":" + sourceItemId, 24);

    private static string CreateHash(string value, int length) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()[..length];
}

/// <summary>Connects one generated custom template with the vanilla template whose loot pools it inherits.</summary>
public sealed record WorldLootInjection(string BaseTemplateId, string CloneTemplateId, double Weight);
