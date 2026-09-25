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
                StimulatorBuffs = stim.Buffs.Any(buff => BuffMappings.IsStimulatorBuff(buff.Effect)) ? ids.BuffKey : string.Empty,
                MaxHpResource = stim.Uses,
                EffectsDamage = CreateDirectStimEffects(stim)
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

    private static Dictionary<DamageEffectType, EffectsDamageProperties>? CreateDirectStimEffects(StimDefinition stim)
    {
        BuffDefinition? painSuppression = stim.Buffs.FirstOrDefault(buff =>
            BuffMappings.TryGet(buff.Effect, out BuffMapping mapping)
            && mapping.ItemEffectType == ItemEffectType.PainSuppression);
        if (painSuppression is null) return null;

        return new Dictionary<DamageEffectType, EffectsDamageProperties>
        {
            [DamageEffectType.Pain] = new()
            {
                Delay = painSuppression.Delay,
                Duration = painSuppression.Duration,
                FadeOut = 0
            }
        };
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

    /// <summary>Creates a custom food item while retaining its vanilla eating behavior.</summary>
    public bool CreateFood(FoodDefinition food, BaseItemMapping baseItem, StimRegistrationIds ids)
    {
        if (!_templateTable.Items.TryGetValue(baseItem.TemplateId, out TemplateItem? sourceItem))
        {
            _logger.LogError("[MoarSupplies] Base template '{TemplateId}' for food '{FoodId}' does not exist in SPT's item table.", baseItem.TemplateId, food.Id);
            return false;
        }
        if (_templateTable.Items.ContainsKey(ids.ItemTemplateId))
        {
            _logger.LogError("[MoarSupplies] Generated item template ID '{ItemTemplateId}' for food '{FoodId}' is already registered.", ids.ItemTemplateId, food.Id);
            return false;
        }

        bool addToHandbook = food.Trader?.Enabled == true;
        HandbookItem? sourceHandbookItem = addToHandbook ? _templateTable.Handbook.Items.FirstOrDefault(item => item.Id == baseItem.TemplateId) : null;
        if (addToHandbook && sourceHandbookItem is null)
        {
            _logger.LogError("[MoarSupplies] Base template '{TemplateId}' for food '{FoodId}' has no handbook entry.", baseItem.TemplateId, food.Id);
            return false;
        }

        Dictionary<HealthFactor, EffectsHealthProperties> nutrition = new();
        if (food.Nutrition.Hydration != 0) nutrition.Add(HealthFactor.Hydration, new EffectsHealthProperties { Value = food.Nutrition.Hydration });
        if (food.Nutrition.Energy != 0) nutrition.Add(HealthFactor.Energy, new EffectsHealthProperties { Value = food.Nutrition.Energy });

        NewItemFromCloneDetails cloneDetails = new()
        {
            ItemTplToClone = baseItem.TemplateId, ParentId = sourceItem.Parent, NewId = ids.ItemTemplateId, NewItemName = food.Identity.Name,
            AddToHandbook = addToHandbook, AddToFleaPriceDb = false, HandbookParentId = sourceHandbookItem?.ParentId.ToString() ?? string.Empty,
            HandbookPriceRoubles = addToHandbook ? food.Trader!.Price : null,
            Locales = new Dictionary<string, LocaleDetails>(StringComparer.OrdinalIgnoreCase) { ["en"] = new LocaleDetails { Name = food.Identity.Name, ShortName = food.Identity.ShortName, Description = food.Identity.Description } },
            OverrideProperties = new TemplateItemProperties { MaxResource = food.Resource, EffectsHealth = nutrition }
        };
        CreateItemResult result = _customItemService.CreateItemFromClone(cloneDetails, typeof(ItemService).Assembly);
        if (!result.Success)
        {
            _logger.LogError("[MoarSupplies] Failed to create food '{FoodId}': {Errors}", food.Id, result.Errors is { Count: > 0 } ? string.Join("; ", result.Errors) : "SPT did not provide an error message.");
            return false;
        }
        if (_debugSettings.Enabled) _logger.LogInformation("[MoarSupplies] Created food '{FoodId}' with template ID '{ItemTemplateId}' from base template '{BaseTemplateId}'.", food.Id, result.ItemId, baseItem.TemplateId);
        return true;
    }

    /// <summary>
    /// Creates a medical-kit clone while retaining all treatment-effect data from
    /// the selected vanilla kit.
    /// </summary>
    public bool CreateMedicalPack(MedicalPackDefinition medicalPack, BaseItemMapping baseItem, StimRegistrationIds ids)
    {
        if (!_templateTable.Items.TryGetValue(baseItem.TemplateId, out TemplateItem? sourceItem))
        {
            _logger.LogError("[MoarSupplies] Base template '{TemplateId}' for medical pack '{MedicalPackId}' does not exist in SPT's item table.", baseItem.TemplateId, medicalPack.Id);
            return false;
        }

        if (_templateTable.Items.ContainsKey(ids.ItemTemplateId))
        {
            _logger.LogError("[MoarSupplies] Generated item template ID '{ItemTemplateId}' for medical pack '{MedicalPackId}' is already registered.", ids.ItemTemplateId, medicalPack.Id);
            return false;
        }

        bool addToHandbook = medicalPack.Trader?.Enabled == true;
        HandbookItem? sourceHandbookItem = addToHandbook
            ? _templateTable.Handbook.Items.FirstOrDefault(item => item.Id == baseItem.TemplateId)
            : null;
        if (addToHandbook && sourceHandbookItem is null)
        {
            _logger.LogError("[MoarSupplies] Base template '{TemplateId}' for medical pack '{MedicalPackId}' has no handbook entry.", baseItem.TemplateId, medicalPack.Id);
            return false;
        }

        double? medUseTime = medicalPack.UseTimeMultiplier is double useTimeMultiplier
            ? sourceItem.Properties?.MedUseTime * useTimeMultiplier
            : null;
        if (medicalPack.UseTimeMultiplier is not null && medUseTime is null)
        {
            _logger.LogError("[MoarSupplies] Medical pack '{MedicalPackId}' requests a use-time multiplier, but base item '{BaseTemplateId}' has no medical use time.", medicalPack.Id, baseItem.TemplateId);
            return false;
        }

        Dictionary<DamageEffectType, EffectsDamageProperties>? surgeryEffects = null;
        if (medicalPack.SurgeryRestoreMultiplier is double surgeryRestoreMultiplier)
        {
            Dictionary<DamageEffectType, EffectsDamageProperties>? sourceEffects = sourceItem.Properties?.EffectsDamage;
            if (sourceEffects is null || !sourceEffects.TryGetValue(DamageEffectType.DestroyedPart, out EffectsDamageProperties? destroyedPart))
            {
                _logger.LogError("[MoarSupplies] Medical pack '{MedicalPackId}' requests a surgery-restoration multiplier, but base item '{BaseTemplateId}' cannot treat destroyed limbs.", medicalPack.Id, baseItem.TemplateId);
                return false;
            }

            surgeryEffects = sourceEffects.ToDictionary(
                effect => effect.Key,
                effect => new EffectsDamageProperties
                {
                    Value = effect.Value.Value,
                    Delay = effect.Value.Delay,
                    Duration = effect.Value.Duration,
                    FadeOut = effect.Value.FadeOut,
                    Cost = effect.Value.Cost,
                    HealthPenaltyMin = effect.Value.HealthPenaltyMin,
                    HealthPenaltyMax = effect.Value.HealthPenaltyMax
                });
            surgeryEffects[DamageEffectType.DestroyedPart].HealthPenaltyMin = destroyedPart.HealthPenaltyMin * surgeryRestoreMultiplier;
            surgeryEffects[DamageEffectType.DestroyedPart].HealthPenaltyMax = destroyedPart.HealthPenaltyMax * surgeryRestoreMultiplier;
        }

        NewItemFromCloneDetails cloneDetails = new()
        {
            ItemTplToClone = baseItem.TemplateId,
            ParentId = sourceItem.Parent,
            NewId = ids.ItemTemplateId,
            NewItemName = medicalPack.Identity.Name,
            AddToHandbook = addToHandbook,
            AddToFleaPriceDb = false,
            HandbookParentId = sourceHandbookItem?.ParentId.ToString() ?? string.Empty,
            HandbookPriceRoubles = addToHandbook ? medicalPack.Trader!.Price : null,
            Locales = new Dictionary<string, LocaleDetails>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new LocaleDetails { Name = medicalPack.Identity.Name, ShortName = medicalPack.Identity.ShortName, Description = medicalPack.Identity.Description }
            },
            OverrideProperties = new TemplateItemProperties
            {
                MaxHpResource = medicalPack.Resource,
                HpResourceRate = medicalPack.ResourceRate,
                MedUseTime = medUseTime,
                EffectsDamage = surgeryEffects
            }
        };

        CreateItemResult result = _customItemService.CreateItemFromClone(cloneDetails, typeof(ItemService).Assembly);
        if (!result.Success)
        {
            _logger.LogError("[MoarSupplies] Failed to create medical pack '{MedicalPackId}': {Errors}", medicalPack.Id, result.Errors is { Count: > 0 } ? string.Join("; ", result.Errors) : "SPT did not provide an error message.");
            return false;
        }

        PreserveDirectSlotEligibility(baseItem.TemplateId, ids.ItemTemplateId);

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation("[MoarSupplies] Created medical pack '{MedicalPackId}' with template ID '{ItemTemplateId}' from base template '{BaseTemplateId}'.", medicalPack.Id, result.ItemId, baseItem.TemplateId);
        }

        return true;
    }

    /// <summary>
    /// Preserves placement in slots whose filters list the vanilla template directly.
    /// Special slots use these direct template-ID allow lists rather than item parents,
    /// so a cloned CMS or Surv12 otherwise loses its native special-slot eligibility.
    /// </summary>
    private void PreserveDirectSlotEligibility(string sourceTemplateId, string cloneTemplateId)
    {
        int updatedFilterCount = 0;

        foreach (TemplateItem template in _templateTable.Items.Values)
        {
            if (template.Properties?.Slots is null) continue;

            foreach (Slot slot in template.Properties.Slots)
            {
                if (slot.Properties?.Filters is null) continue;

                foreach (SlotFilter filter in slot.Properties.Filters)
                {
                    if (filter.Filter is null || !filter.Filter.Contains(sourceTemplateId)) continue;
                    if (!filter.Filter.Add(cloneTemplateId)) continue;

                    updatedFilterCount++;
                }
            }
        }

        if (_debugSettings.Enabled && updatedFilterCount > 0)
        {
            _logger.LogInformation(
                "[MoarSupplies] Preserved direct slot eligibility for clone '{CloneTemplateId}' from source '{SourceTemplateId}' in {FilterCount} filter(s).",
                cloneTemplateId,
                sourceTemplateId,
                updatedFilterCount);
        }
    }
}
