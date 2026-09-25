using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;

namespace MoarSupplies.Services;

/// <summary>Registers a validated food item with its immediate nutrition.</summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class FoodService
{
    private readonly ILogger<FoodService> _logger;
    private readonly BaseItemResolver _baseItemResolver;
    private readonly ItemService _itemService;
    private readonly StimIdService _idService;
    private readonly TraderService _traderService;

    public FoodService(ILogger<FoodService> logger, BaseItemResolver baseItemResolver, ItemService itemService, StimIdService idService, TraderService traderService)
    {
        _logger = logger;
        _baseItemResolver = baseItemResolver;
        _itemService = itemService;
        _idService = idService;
        _traderService = traderService;
    }

    public bool Register(FoodDefinition food)
    {
        if (!_baseItemResolver.TryResolveFood(food.BaseItem, out BaseItemMapping baseItem))
        {
            _logger.LogError("[MoarSupplies] Could not resolve base item '{BaseItem}' for food '{FoodId}'.", food.BaseItem, food.Id);
            return false;
        }

        StimRegistrationIds ids = _idService.CreateFood(food.Id);
        return _itemService.CreateFood(food, baseItem, ids) && _traderService.Register(food, ids);
    }
}
