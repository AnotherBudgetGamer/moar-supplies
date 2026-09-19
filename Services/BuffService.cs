using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>
/// Converts friendly stim effects into the representation expected by SPT.
/// Registration in the global stimulator buff table is handled by a later milestone.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class BuffService
{
    private readonly GlobalTable _globalTable;

    public BuffService(GlobalTable globalTable)
    {
        _globalTable = globalTable;
    }

    public bool IsRegistered(string buffKey) =>
        _globalTable.Configuration.Health.Effects.Stimulator.Buffs.ContainsKey(buffKey);

    public Buff Convert(BuffDefinition definition)
    {
        if (!BuffMappings.TryGet(definition.Effect, out BuffMapping mapping))
        {
            throw new ArgumentException(
                $"Effect '{definition.Effect}' is not supported.",
                nameof(definition));
        }

        if (mapping.RequiresValue && definition.Value is null)
        {
            throw new ArgumentException(
                $"Effect '{definition.Effect}' requires a value.",
                nameof(definition));
        }

        return new Buff
        {
            BuffType = mapping.BuffType,
            SkillName = mapping.SkillName ?? string.Empty,
            Chance = 1,
            Delay = definition.Delay,
            Duration = definition.Duration,
            Value = definition.Value ?? 0,
            AbsoluteValue = mapping.AbsoluteValue
        };
    }

    public void Register(string buffKey, IEnumerable<Buff> buffs)
    {
        _globalTable.Configuration.Health.Effects.Stimulator.Buffs.Add(buffKey, buffs.ToArray());
    }
}
