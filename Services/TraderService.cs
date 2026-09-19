using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>
/// Adds custom stims to a configured SPT trader's assort.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class TraderService
{
    private const string RoublesTemplateId = "5449016a4bdc2d6f028b456f";
    private const string TraderRoot = "hideout";

    private readonly ILogger<TraderService> _logger;
    private readonly DebugSettings _debugSettings;
    private readonly TradersTable _tradersTable;

    public TraderService(ILogger<TraderService> logger, DebugSettings debugSettings, TradersTable tradersTable)
    {
        _logger = logger;
        _debugSettings = debugSettings;
        _tradersTable = tradersTable;
    }

    public bool Register(StimDefinition stim, StimRegistrationIds ids)
    {
        TraderDefinition? sale = stim.Trader;
        if (sale?.Enabled != true)
        {
            return true;
        }

        if (!TraderMappings.TryGet(sale.Trader, out TraderMapping traderMapping))
        {
            _logger.LogError(
                "[MoarSupplies] Could not resolve trader '{TraderName}' for stim '{StimId}'.",
                sale.Trader,
                stim.Id);
            return false;
        }

        Trader? trader;
        try
        {
            trader = _tradersTable.GetTrader(traderMapping.TraderId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                "[MoarSupplies] Could not load trader '{TraderName}' ({TraderId}) for stim '{StimId}': {Message}",
                sale.Trader,
                traderMapping.TraderId,
                stim.Id,
                exception.Message);
            return false;
        }

        if (trader is null)
        {
            _logger.LogError(
                "[MoarSupplies] Trader '{TraderName}' ({TraderId}) was not found for stim '{StimId}'.",
                sale.Trader,
                traderMapping.TraderId,
                stim.Id);
            return false;
        }

        if (trader.Assort.Items.Any(item => item.Id == ids.TraderAssortId)
            || trader.Assort.BarterScheme.ContainsKey(ids.TraderAssortId)
            || trader.Assort.LoyalLevelItems.ContainsKey(ids.TraderAssortId))
        {
            _logger.LogError(
                "[MoarSupplies] Generated assort ID '{AssortId}' for stim '{StimId}' is already registered with trader '{TraderName}'.",
                ids.TraderAssortId,
                stim.Id,
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
                MedKit = new UpdMedKit
                {
                    HpResource = stim.Uses
                }
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
                "[MoarSupplies] Added stim '{StimId}' to trader '{TraderName}' at loyalty level {LoyaltyLevel} for {Price} roubles (assort ID '{AssortId}').",
                stim.Id,
                sale.Trader,
                sale.LoyaltyLevel,
                sale.Price,
                ids.TraderAssortId);
        }
        return true;
    }
}
