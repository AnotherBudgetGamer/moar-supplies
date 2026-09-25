using Microsoft.Extensions.Logging;
using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>
/// Coordinates registration of one fully validated stim definition.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class StimService
{
    private readonly ILogger<StimService> _logger;
    private readonly DebugSettings _debugSettings;
    private readonly BaseItemResolver _baseItemResolver;
    private readonly BuffService _buffService;
    private readonly ItemService _itemService;
    private readonly StimIdService _stimIdService;
    private readonly TraderService _traderService;

    public StimService(
        ILogger<StimService> logger,
        DebugSettings debugSettings,
        BaseItemResolver baseItemResolver,
        BuffService buffService,
        ItemService itemService,
        StimIdService stimIdService,
        TraderService traderService)
    {
        _logger = logger;
        _debugSettings = debugSettings;
        _baseItemResolver = baseItemResolver;
        _buffService = buffService;
        _itemService = itemService;
        _stimIdService = stimIdService;
        _traderService = traderService;
    }

    public bool Register(StimDefinition stim)
    {
        if (!_baseItemResolver.TryResolve(stim.BaseItem, out BaseItemMapping baseItem))
        {
            _logger.LogError(
                "[MoarSupplies] Could not resolve base item '{BaseItem}' for stim '{StimId}'.",
                stim.BaseItem,
                stim.Id);
            return false;
        }

        StimRegistrationIds ids = _stimIdService.Create(stim.Id);
        if (_buffService.IsRegistered(ids.BuffKey))
        {
            _logger.LogError(
                "[MoarSupplies] Buff key '{BuffKey}' for stim '{StimId}' is already registered.",
                ids.BuffKey,
                stim.Id);
            return false;
        }

        List<Buff> buffs = [];
        for (int buffIndex = 0; buffIndex < stim.Buffs.Count; buffIndex++)
        {
            if (!BuffMappings.IsStimulatorBuff(stim.Buffs[buffIndex].Effect)) continue;

            Buff buff = _buffService.Convert(stim.Buffs[buffIndex]);
            buffs.Add(buff);

            if (_debugSettings.Enabled)
            {
                _logger.LogInformation(
                    "[MoarSupplies] Converted stim '{StimId}' buff {BuffIndex}: effect '{Effect}' -> BuffType '{BuffType}', SkillName '{SkillName}', Value {Value}, Duration {Duration}, Delay {Delay}.",
                    stim.Id,
                    buffIndex,
                    stim.Buffs[buffIndex].Effect,
                    buff.BuffType,
                    string.IsNullOrEmpty(buff.SkillName) ? "(none)" : buff.SkillName,
                    buff.Value,
                    buff.Duration,
                    buff.Delay);
            }
        }

        if (!_itemService.CreateStim(stim, baseItem, ids))
        {
            return false;
        }

        if (buffs.Count > 0) _buffService.Register(ids.BuffKey, buffs);
        if (_debugSettings.Enabled)
        {
            _logger.LogInformation(
                "[MoarSupplies] Registered {BuffCount} buff(s) under key '{BuffKey}' for stim '{StimId}'.",
                buffs.Count,
                ids.BuffKey,
                stim.Id);
        }

        if (!_traderService.Register(stim, ids))
        {
            return false;
        }

        return true;
    }
}
