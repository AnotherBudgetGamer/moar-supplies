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
/// Creates a custom stim by cloning a supported SPT base item.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class ItemService
{
    private readonly ILogger<ItemService> _logger;
    private readonly DebugSettings _debugSettings;
    private readonly CustomItemService _customItemService;
    private readonly TemplateTable _templateTable;

    public ItemService(
        ILogger<ItemService> logger,
        DebugSettings debugSettings,
        CustomItemService customItemService,
        TemplateTable templateTable)
    {
        _logger = logger;
        _debugSettings = debugSettings;
        _customItemService = customItemService;
        _templateTable = templateTable;
    }

    public bool CreateStim(
        StimDefinition stim,
        BaseItemMapping baseItem,
        StimRegistrationIds ids)
    {
        if (!_templateTable.Items.TryGetValue(baseItem.TemplateId, out TemplateItem? sourceItem))
        {
            _logger.LogError(
                "[MoarSupplies] Base template '{TemplateId}' for stim '{StimId}' does not exist in SPT's item table.",
                baseItem.TemplateId,
                stim.Id);
            return false;
        }

        if (_templateTable.Items.ContainsKey(ids.ItemTemplateId))
        {
            _logger.LogError(
                "[MoarSupplies] Generated item template ID '{ItemTemplateId}' for stim '{StimId}' is already registered.",
                ids.ItemTemplateId,
                stim.Id);
            return false;
        }

        bool addToHandbook = stim.Trader?.Enabled == true;
        HandbookItem? sourceHandbookItem = null;
        if (addToHandbook)
        {
            sourceHandbookItem = _templateTable.Handbook.Items.FirstOrDefault(item => item.Id == baseItem.TemplateId);
            if (sourceHandbookItem is null)
            {
                _logger.LogError(
                    "[MoarSupplies] Base template '{TemplateId}' for stim '{StimId}' has no handbook entry.",
                    baseItem.TemplateId,
                    stim.Id);
                return false;
            }
        }

        NewItemFromCloneDetails cloneDetails = new()
        {
            ItemTplToClone = baseItem.TemplateId,
            ParentId = sourceItem.Parent,
            NewId = ids.ItemTemplateId,
            NewItemName = stim.Identity.Name,
            AddToHandbook = addToHandbook,
            AddToFleaPriceDb = false,
            HandbookParentId = sourceHandbookItem?.ParentId.ToString() ?? string.Empty,
            HandbookPriceRoubles = addToHandbook ? stim.Trader!.Price : null,
            Locales = new Dictionary<string, LocaleDetails>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new LocaleDetails
                {
                    Name = stim.Identity.Name,
                    ShortName = stim.Identity.ShortName,
                    Description = stim.Identity.Description
                }
            },
            OverrideProperties = new TemplateItemProperties
            {
                StimulatorBuffs = ids.BuffKey,
                MaxHpResource = stim.Uses
            }
        };

        CreateItemResult result = _customItemService.CreateItemFromClone(cloneDetails, typeof(ItemService).Assembly);
        if (!result.Success)
        {
            string errors = result.Errors is { Count: > 0 }
                ? string.Join("; ", result.Errors)
                : "SPT did not provide an error message.";

            _logger.LogError(
                "[MoarSupplies] Failed to create stim '{StimId}': {Errors}",
                stim.Id,
                errors);
            return false;
        }

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation(
                "[MoarSupplies] Created stim '{StimId}' with template ID '{ItemTemplateId}' from base template '{BaseTemplateId}'.",
                stim.Id,
                result.ItemId,
                baseItem.TemplateId);
        }
        return true;
    }
}
