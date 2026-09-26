using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace MoarSupplies.Services;

/// <summary>
/// Builds a complete, deterministic smoke-test set for the vanilla templates
/// referenced by <see cref="ItemMappings"/>. It deliberately uses the same
/// SPT clone API as normal Moar Supplies registrations, so a failure identifies
/// an invalid source mapping instead of merely checking that an ID exists.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class MappedItemTestCloneService
{
    private const double TestValueMultiplier = 1.2;

    private readonly ILogger<MappedItemTestCloneService> _logger;
    private readonly CustomItemService _customItemService;
    private readonly TemplateTable _templateTable;
    private readonly LocaleTable _localeTable;
    private readonly StimIdService _stimIdService;

    public MappedItemTestCloneService(
        ILogger<MappedItemTestCloneService> logger,
        CustomItemService customItemService,
        TemplateTable templateTable,
        LocaleTable localeTable,
        StimIdService stimIdService)
    {
        _logger = logger;
        _customItemService = customItemService;
        _templateTable = templateTable;
        _localeTable = localeTable;
        _stimIdService = stimIdService;
    }

    public void CreateMissingMappedItemClones(ModConfig config)
    {
        if (!config.EnableMappedItemTestClones) return;

        HashSet<string> starterSourceIds = GetStarterSourceIds(config);
        int created = 0;
        int skipped = 0;
        int failed = 0;

        foreach ((string mappingName, BaseItemMapping mapping) in ItemMappings.AllMappedItems.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
        {
            string sourceTemplateId = mapping.TemplateId;
            if (starterSourceIds.Contains(sourceTemplateId))
            {
                skipped++;
                continue;
            }

            if (!TryCreateClone(mappingName, sourceTemplateId)) failed++;
            else created++;
        }

        _logger.LogInformation(
            "[MoarSupplies] Mapping test clones complete: {CreatedCount} created, {StarterCount} starter-pack sources skipped, {FailedCount} failed.",
            created,
            skipped,
            failed);
    }

    private bool TryCreateClone(string mappingName, string sourceTemplateId)
    {
        if (!_templateTable.Items.TryGetValue(sourceTemplateId, out TemplateItem? sourceItem))
        {
            _logger.LogError("[MoarSupplies] Mapping test clone '{MappingName}' failed: source template '{SourceTemplateId}' does not exist.", mappingName, sourceTemplateId);
            return false;
        }

        string cloneTemplateId = _stimIdService.CreateMappedItemTestClone(sourceTemplateId);
        if (_templateTable.Items.ContainsKey(cloneTemplateId))
        {
            _logger.LogInformation("[MoarSupplies] Mapping test clone '{MappingName}' already exists as '{CloneTemplateId}'.", mappingName, cloneTemplateId);
            return true;
        }

        HandbookItem? handbookItem = _templateTable.Handbook.Items.FirstOrDefault(item => item.Id == sourceTemplateId);
        // A few valid items (notably Sanitar's special medical items) are
        // intentionally absent from the vanilla handbook. They still need to
        // participate in the mapping test, so use their template parent as the
        // diagnostic handbook category instead of excluding them.
        string handbookParentId = handbookItem?.ParentId.ToString() ?? sourceItem.Parent.ToString();
        if (handbookItem is null)
        {
            _logger.LogInformation("[MoarSupplies] Mapping test source '{MappingName}' has no vanilla handbook entry; using parent '{HandbookParentId}' for its diagnostic listing.", mappingName, handbookParentId);
        }

        // Template locale records are not authoritative in installations with
        // content-backport mods: several records are shared or contain raw
        // item_* keys. The mapping key is the source identity we are testing,
        // so use it for the diagnostic label rather than letting a bad locale
        // make a correct clone look like a mapping error.
        string displayName = GetMappingDisplayName(mappingName);
        string shortName = displayName;
        string description = GetEnglishLocaleValue(sourceTemplateId, "Description", sourceItem.Properties?.Description, string.Empty);
        int handbookPrice = _templateTable.Prices.TryGetValue(sourceTemplateId, out double sourcePrice)
            ? (int)Math.Ceiling(sourcePrice * TestValueMultiplier)
            : 1;

        NewItemFromCloneDetails cloneDetails = new()
        {
            ItemTplToClone = sourceTemplateId,
            ParentId = sourceItem.Parent,
            NewId = cloneTemplateId,
            NewItemName = $"Moar {displayName}",
            AddToHandbook = true,
            AddToFleaPriceDb = true,
            HandbookParentId = handbookParentId,
            HandbookPriceRoubles = handbookPrice,
            FleaPriceRoubles = handbookPrice,
            Locales = CreateTestLocales(displayName, shortName, description)
        };

        CreateItemResult result;
        try
        {
            result = _customItemService.CreateItemFromClone(cloneDetails, typeof(MappedItemTestCloneService).Assembly);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "[MoarSupplies] Mapping test clone '{MappingName}' from '{SourceTemplateId}' threw during registration. The remaining mappings will still be tested.", mappingName, sourceTemplateId);
            return false;
        }
        if (!result.Success)
        {
            string errors = result.Errors is { Count: > 0 } ? string.Join("; ", result.Errors) : "SPT did not provide an error message.";
            _logger.LogError("[MoarSupplies] Mapping test clone '{MappingName}' from '{SourceTemplateId}' failed: {Errors}", mappingName, sourceTemplateId, errors);
            return false;
        }

        if (!_templateTable.Items.TryGetValue(cloneTemplateId, out TemplateItem? cloneItem) || cloneItem.Properties is null)
        {
            _logger.LogError("[MoarSupplies] Mapping test clone '{MappingName}' was created but its template properties could not be found.", mappingName);
            return false;
        }

        MultiplyNumericValues(cloneItem.Properties, new HashSet<object>(ReferenceEqualityComparer.Instance));
        _logger.LogInformation("[MoarSupplies] Mapping test clone '{MappingName}' created: '{CloneTemplateId}' from '{SourceTemplateId}'.", mappingName, cloneTemplateId, sourceTemplateId);
        return true;
    }

    private static HashSet<string> GetStarterSourceIds(ModConfig config)
    {
        HashSet<string> sourceIds = new(StringComparer.OrdinalIgnoreCase);
        AddResolved(config.Stims.Select(item => item.BaseItem), ItemMappings.TryGet);
        AddResolved(config.Drinks.Select(item => item.BaseItem), ItemMappings.TryGetDrink);
        AddResolved(config.Foods.Select(item => item.BaseItem), ItemMappings.TryGetFood);
        AddResolved(config.MedicalPacks.Select(item => item.BaseItem), ItemMappings.TryGetMedicalPack);
        return sourceIds;

        void AddResolved(IEnumerable<string> baseItems, TryResolveMapping resolver)
        {
            foreach (string baseItem in baseItems)
            {
                if (resolver(baseItem, out BaseItemMapping mapping)) sourceIds.Add(mapping.TemplateId);
            }
        }
    }

    private delegate bool TryResolveMapping(string baseItem, out BaseItemMapping mapping);

    private static string GetMappingDisplayName(string mappingName) => mappingName switch
    {
        "2a2btg" => "2A2-BTG",
        "3btg" => "3-BTG",
        "ahf1m" => "AHF1-M",
        "etgchange" => "eTG-change",
        "l1" => "L1",
        "mule" => "M.U.L.E.",
        "p22" => "P22",
        "pnb" => "PNB",
        "sj1" => "SJ1",
        "sj6" => "SJ6",
        "sj9" => "SJ9",
        "sj12" => "SJ12",
        "xtg12" => "X-TG-12",
        "calok-b" => "CALOK-B",
        "afak" => "AFAK",
        "ai2" => "AI-2",
        "ifak" => "IFAK",
        "sanitar-afak" => "Sanitar AFAK",
        "sanitar-surgery-kit" => "Sanitar Surgery Kit",
        "salty-dog-sausage" => "Sausage",
        _ => string.Join(' ', mappingName
            .Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]))
    };

    private Dictionary<string, LocaleDetails> CreateTestLocales(string displayName, string shortName, string description)
    {
        LocaleDetails text = new()
        {
            Name = $"Moar {displayName}",
            ShortName = $"M-{shortName}",
            Description = description
        };

        Dictionary<string, LocaleDetails> locales = new(StringComparer.OrdinalIgnoreCase);
        foreach (string locale in _localeTable.Global.Keys)
        {
            locales[locale] = text;
        }

        // A minimal test install may only load its active locale. SPT accepts
        // this explicit English fallback even when it is absent from Global.
        locales.TryAdd("en", text);
        return locales;
    }

    private string GetEnglishLocaleValue(string templateId, string field, string? templateFallback, string finalFallback)
    {
        if (_localeTable.Global.TryGetValue("en", out var englishLocale)
            && englishLocale.Value?.ExtensionData is { } englishEntries
            && englishEntries.TryGetValue($"{templateId} {field}", out object? value)
            && value is string text
            && !string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return string.IsNullOrWhiteSpace(templateFallback) ? finalFallback : templateFallback;
    }

    /// <summary>
    /// Applies the requested +20% to every writable numeric property in the
    /// cloned item graph. Strings, IDs, enums, booleans, and item filters are
    /// intentionally untouched, preserving the source item's client assets and
    /// compatibility rules.
    /// </summary>
    private static void MultiplyNumericValues(object value, HashSet<object> visited)
    {
        if (!visited.Add(value)) return;

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is null) continue;
                if (TryMultiply(entry.Value, entry.Value.GetType(), out object? multiplied)) dictionary[entry.Key] = multiplied;
                else Traverse(entry.Value, visited);
            }
            return;
        }

        if (value is IEnumerable enumerable and not string)
        {
            foreach (object? item in enumerable)
            {
                if (item is not null) Traverse(item, visited);
            }
            return;
        }

        foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0) continue;

            object? propertyValue = property.GetValue(value);
            if (propertyValue is null) continue;
            if (TryMultiply(propertyValue, property.PropertyType, out object? multiplied))
            {
                property.SetValue(value, multiplied);
                continue;
            }

            Traverse(propertyValue, visited);
        }
    }

    private static void Traverse(object value, HashSet<object> visited)
    {
        Type type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string || value is decimal) return;
        MultiplyNumericValues(value, visited);
    }

    private static bool TryMultiply(object value, Type declaredType, out object? multiplied)
    {
        Type type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        multiplied = type == typeof(byte) ? checked((byte)Math.Ceiling((byte)value * TestValueMultiplier))
            : type == typeof(short) ? checked((short)Math.Ceiling((short)value * TestValueMultiplier))
            : type == typeof(int) ? checked((int)Math.Ceiling((int)value * TestValueMultiplier))
            : type == typeof(long) ? checked((long)Math.Ceiling((long)value * TestValueMultiplier))
            : type == typeof(float) ? (float)((float)value * TestValueMultiplier)
            : type == typeof(double) ? (double)value * TestValueMultiplier
            : type == typeof(decimal) ? (decimal)value * (decimal)TestValueMultiplier
            : null;
        return multiplied is not null;
    }
}
