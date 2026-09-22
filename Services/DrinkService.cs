using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>
/// Registers a validated drink, including its immediate nutrition and optional timed effects.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class DrinkService
{
    private readonly ILogger<DrinkService> _logger;
    private readonly BaseItemResolver _baseItemResolver;
    private readonly BuffService _buffService;
    private readonly ItemService _itemService;
    private readonly StimIdService _idService;
    private readonly TraderService _traderService;

    public DrinkService(ILogger<DrinkService> logger, BaseItemResolver baseItemResolver, BuffService buffService, ItemService itemService, StimIdService idService, TraderService traderService)
    {
        _logger = logger;
        _baseItemResolver = baseItemResolver;
        _buffService = buffService;
        _itemService = itemService;
        _idService = idService;
        _traderService = traderService;
    }

    public bool Register(DrinkDefinition drink)
    {
        if (!_baseItemResolver.TryResolveDrink(drink.BaseItem, out BaseItemMapping baseItem))
        {
            _logger.LogError("[MoarSupplies] Could not resolve base item '{BaseItem}' for drink '{DrinkId}'.", drink.BaseItem, drink.Id);
            return false;
        }

        StimRegistrationIds ids = _idService.CreateDrink(drink.Id);
        if (_buffService.IsRegistered(ids.BuffKey))
        {
            _logger.LogError("[MoarSupplies] Buff key '{BuffKey}' for drink '{DrinkId}' is already registered.", ids.BuffKey, drink.Id);
            return false;
        }

        if (!_itemService.CreateDrink(drink, baseItem, ids)) return false;
        if (drink.Buffs.Count > 0) _buffService.Register(ids.BuffKey, drink.Buffs.Select(_buffService.Convert));
        return _traderService.Register(drink, ids);
    }
}
