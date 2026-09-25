using Microsoft.Extensions.Logging;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>
/// Adds configured Moar Supplies stimulants to an SPT trader's assort.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class TraderService
{
    private const string RoublesTemplateId = "5449016a4bdc2d6f028b456f";
    private const string TraderRoot = "hideout";

    private readonly ILogger<TraderService> _logger;
    private readonly DebugSettings _debugSettings;
    private readonly TradersTable _tradersTable;
    private readonly TraderDirectory _traderDirectory;

    public TraderService(ILogger<TraderService> logger, DebugSettings debugSettings, TradersTable tradersTable, TraderDirectory traderDirectory)
    {
        _logger = logger;
        _debugSettings = debugSettings;
        _tradersTable = tradersTable;
        _traderDirectory = traderDirectory;
    }

    public bool Register(StimDefinition stim, StimRegistrationIds ids)
        => Register(stim.Id, stim.Trader, ids, new UpdMedKit { HpResource = stim.Uses }, "stim");

    public bool Register(DrinkDefinition drink, StimRegistrationIds ids)
        => Register(drink.Id, drink.Trader, ids, new UpdResource { Value = drink.Resource }, "drink");

    public bool Register(FoodDefinition food, StimRegistrationIds ids)
        => Register(food.Id, food.Trader, ids, new UpdResource { Value = food.Resource }, "food");

    public bool Register(MedicalPackDefinition medicalPack, StimRegistrationIds ids)
        => Register(medicalPack.Id, medicalPack.Trader, ids, new UpdMedKit { HpResource = medicalPack.Resource }, "medical pack");

    private bool Register(string definitionId, TraderDefinition? sale, StimRegistrationIds ids, object resource, string definitionType)
    {
        if (sale?.Enabled != true)
        {
            return true;
        }

        if (!_traderDirectory.TryResolve(sale, out string traderId))
        {
            _logger.LogError(
                "[MoarSupplies] Could not resolve trader '{TraderName}' ({TraderId}) for {DefinitionType} '{DefinitionId}'.",
                sale.Trader,
                sale.TraderId ?? "legacy selection",
                definitionType,
                definitionId);
            return false;
        }

        Trader? trader;
        try
        {
            trader = _tradersTable.GetTrader(traderId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "[MoarSupplies] Could not load trader '{TraderName}' ({TraderId}) for {DefinitionType} '{DefinitionId}': {Message}",
                sale.Trader,
                traderId,
                definitionType,
                definitionId,
                exception.Message);
            return false;
        }

        if (trader is null)
        {
            _logger.LogError(
                "[MoarSupplies] Trader '{TraderName}' ({TraderId}) was not found for {DefinitionType} '{DefinitionId}'.",
                sale.Trader,
                traderId,
                definitionType,
                definitionId);
            return false;
        }

        if (trader.Assort.Items.Any(item => item.Id == ids.TraderAssortId)
            || trader.Assort.BarterScheme.ContainsKey(ids.TraderAssortId)
            || trader.Assort.LoyalLevelItems.ContainsKey(ids.TraderAssortId))
        {
            _logger.LogError(
                "[MoarSupplies] Generated assort ID '{AssortId}' for {DefinitionType} '{DefinitionId}' is already registered with trader '{TraderName}'.",
                ids.TraderAssortId,
                definitionType,
                definitionId,
                sale.Trader);
            return false;
        }

        Item assortItem = new()
        {
            Id = ids.TraderAssortId,
            Template = ids.ItemTemplateId,
            ParentId = TraderRoot,
            SlotId = TraderRoot,
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9999999,
                MedKit = resource as UpdMedKit,
                Resource = resource as UpdResource
            }
        };

        List<List<BarterScheme>> barterScheme =
        [
            [
                new BarterScheme
                {
                    Count = sale.Price,
                    Template = RoublesTemplateId
                }
            ]
        ];

        trader.Assort.Items.Add(assortItem);
        trader.Assort.BarterScheme.Add(ids.TraderAssortId, barterScheme);
        trader.Assort.LoyalLevelItems.Add(ids.TraderAssortId, sale.LoyaltyLevel);

        if (_debugSettings.Enabled)
        {
            _logger.LogInformation(
                "[MoarSupplies] Added {DefinitionType} '{DefinitionId}' to trader '{TraderName}' at loyalty level {LoyaltyLevel} for {Price} roubles (assort ID '{AssortId}').",
                definitionType,
                definitionId,
                sale.Trader,
                sale.LoyaltyLevel,
                sale.Price,
                ids.TraderAssortId);
        }
        return true;
    }
}
