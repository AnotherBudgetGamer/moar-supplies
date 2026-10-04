using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;

namespace MoarSupplies.Services;

/// <summary>
/// Provides a live, localized catalog of installed SPT item templates for the
/// crafting editor. No hand-maintained ingredient mapping is required.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class ItemCatalogService
{
    private readonly TemplateTable _templateTable;
    private readonly LocaleService _localeService;
    private IReadOnlyList<CraftIngredientOption>? _items;

    public ItemCatalogService(TemplateTable templateTable, LocaleService localeService)
    {
        _templateTable = templateTable;
        _localeService = localeService;
    }

    public IReadOnlyList<CraftIngredientOption> Search(string? query, int maximumResults = 20)
    {
        string normalizedQuery = Normalize(query);
        if (string.IsNullOrEmpty(normalizedQuery)) return [];

        return GetItems()
            .Where(item => Normalize(item.Name).Contains(normalizedQuery, StringComparison.Ordinal)
                || Normalize(item.ShortName).Contains(normalizedQuery, StringComparison.Ordinal)
                || item.TemplateId.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => StartsWith(item, normalizedQuery) ? 0 : 1)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maximumResults)
            .ToArray();
    }

    public string GetDisplayName(string templateId) =>
        GetItems().FirstOrDefault(item => string.Equals(item.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))?.DisplayName
        ?? templateId;

    private IReadOnlyList<CraftIngredientOption> GetItems()
    {
        if (_items is not null) return _items;

        // LocaleTable.Global is a lazy backing store. SPT explicitly requires
        // consumers to use LocaleService so its locale data is materialized.
        Dictionary<string, string> localeEntries = _localeService.GetLocaleDb(_localeService.GetDesiredGameLocale());

        _items = _templateTable.Items.Keys
            .Select(templateId => new
            {
                TemplateId = templateId,
                Name = GetLocaleValue(localeEntries, $"{templateId} Name"),
                ShortName = GetLocaleValue(localeEntries, $"{templateId} ShortName")
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new CraftIngredientOption(item.TemplateId, item.Name!, item.ShortName ?? string.Empty))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return _items;
    }

    private static string? GetLocaleValue(IReadOnlyDictionary<string, string> entries, string key) =>
        entries.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static bool StartsWith(CraftIngredientOption item, string normalizedQuery) =>
        Normalize(item.Name).StartsWith(normalizedQuery, StringComparison.Ordinal)
        || Normalize(item.ShortName).StartsWith(normalizedQuery, StringComparison.Ordinal);

    private static string Normalize(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsLetterOrDigit)).ToLowerInvariant();
}
