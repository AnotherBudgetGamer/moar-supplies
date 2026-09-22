using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Enums;
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

    /// <summary>
    /// Creates a custom drink by retaining a vanilla drink's client-side assets and
    /// consumption behavior, while replacing its resource and nutrition values.
    /// </summary>
    public bool CreateDrink(DrinkDefinition drink, BaseItemMapping baseItem, StimRegistrationIds ids)
    {
        if (!_templateTable.Items.TryGetValue(baseItem.TemplateId, out TemplateItem? sourceItem))
        {
            _logger.LogError("[MoarSupplies] Base template '{TemplateId}' for drink '{DrinkId}' does not exist in SPT's item table.", baseItem.TemplateId, drink.Id);
            return false;
        }

        if (_templateTable.Items.ContainsKey(ids.ItemTemplateId))
        {
            _logger.LogError("[MoarSupplies] Generated item template ID '{ItemTemplateId}' for drink '{DrinkId}' is already registered.", ids.ItemTemplateId, drink.Id);
            return false;
        }

        bool addToHandbook = drink.Trader?.Enabled == true;
        HandbookItem? sourceHandbookItem = addToHandbook
            ? _templateTable.Handbook.Items.FirstOrDefault(item => item.Id == baseItem.TemplateId)
            : null;
        if (addToHandbook && sourceHandbookItem is null)
        {
            _logger.LogError("[MoarSupplies] Base template '{TemplateId}' for drink '{DrinkId}' has no handbook entry.", baseItem.TemplateId, drink.Id);
            return false;
        }

        Dictionary<HealthFactor, EffectsHealthProperties> nutrition = new();
        if (drink.Nutrition.Hydration != 0)
        {
            nutrition.Add(HealthFactor.Hydration, new EffectsHealthProperties { Value = drink.Nutrition.Hydration });
        }
        if (drink.Nutrition.Energy != 0)
        {
            nutrition.Add(HealthFactor.Energy, new EffectsHealthProperties { Value = drink.Nutrition.Energy });
        }

        NewItemFromCloneDetails cloneDetails = new()
        {
            ItemTplToClone = baseItem.TemplateId,
            ParentId = sourceItem.Parent,
            NewId = ids.ItemTemplateId,
            NewItemName = drink.Identity.Name,
            AddToHandbook = addToHandbook,
            AddToFleaPriceDb = false,
            HandbookParentId = sourceHandbookItem?.ParentId.ToString() ?? string.Empty,
            HandbookPriceRoubles = addToHandbook ? drink.Trader!.Price : null,
            Locales = new Dictionary<string, LocaleDetails>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new LocaleDetails { Name = drink.Identity.Name, ShortName = drink.Identity.ShortName, Description = drink.Identity.Description }
            },
            OverrideProperties = new TemplateItemProperties
            {
                MaxResource = drink.Resource,
                EffectsHealth = nutrition,
                StimulatorBuffs = drink.Buffs.Count > 0 ? ids.BuffKey : string.Empty
            }
        };

        CreateItemResult result = _customItemService.CreateItemFromClone(cloneDetails, typeof(ItemService).Assembly);
        if (!result.Success)
        {
            _logger.LogError("[MoarSupplies] Failed to create drink '{DrinkId}': {Errors}", drink.Id, result.Errors is { Count: > 0 } ? string.Join("; ", result.Errors) : "SPT did not provide an error message.");
            return false;
        }

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation(
                "[MoarSupplies] Created drink '{DrinkId}' with template ID '{ItemTemplateId}' from base template '{BaseTemplateId}'.",
                drink.Id,
                result.ItemId,
                baseItem.TemplateId);
        }
        return true;
    }
}
