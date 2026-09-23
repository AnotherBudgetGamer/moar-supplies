using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;

namespace MoarSupplies.Services;

/// <summary>
/// Registers validated medical-kit clones with their original treatment behavior.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class MedicalPackService
{
    private readonly ILogger<MedicalPackService> _logger;
    private readonly BaseItemResolver _baseItemResolver;
    private readonly ItemService _itemService;
    private readonly StimIdService _idService;
    private readonly TraderService _traderService;

    public MedicalPackService(ILogger<MedicalPackService> logger, BaseItemResolver baseItemResolver, ItemService itemService, StimIdService idService, TraderService traderService)
    {
        _logger = logger;
        _baseItemResolver = baseItemResolver;
        _itemService = itemService;
        _idService = idService;
        _traderService = traderService;
    }

    public bool Register(MedicalPackDefinition medicalPack)
    {
        if (!_baseItemResolver.TryResolveMedicalPack(medicalPack.BaseItem, out BaseItemMapping baseItem))
        {
            _logger.LogError("[MoarSupplies] Could not resolve base item '{BaseItem}' for medical pack '{MedicalPackId}'.", medicalPack.BaseItem, medicalPack.Id);
            return false;
        }

        StimRegistrationIds ids = _idService.CreateMedicalPack(medicalPack.Id);
        if (!_itemService.CreateMedicalPack(medicalPack, baseItem, ids)) return false;
        return _traderService.Register(medicalPack, ids);
    }
}
