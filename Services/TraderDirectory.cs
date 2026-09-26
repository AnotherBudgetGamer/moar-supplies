using MoarSupplies.Definitions;
using MoarSupplies.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace MoarSupplies.Services;

/// <summary>
/// Reads the traders registered in the current SPT server, including traders added by other mods.
/// </summary>
[Injectable(InjectionType.Singleton, 0)]
public sealed class TraderDirectory
{
    private static readonly IReadOnlyDictionary<string, string> DisplayNameOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // SPT's base trader data uses "Arena" as Ref's nickname, while the game presents this trader as Ref.
        ["6617beeaa9cfa777ca915b7c"] = "Ref",
        // Fence uses a randomized SPT assort rather than a conventional static trader inventory.
        ["579dc571d53a0658a154fbec"] = "Fence *"
    };

    private static readonly ISet<string> ExcludedTraderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // These SPT records do not expose conventional trader assort screens.
        "6864e812f9fe664cb8b8e152", // Storyteller: quest and reward service
        "656f0f98d80a697f855d34b1", // BTR: in-raid vehicle service
        "638f541a29ffd1183d187f57"  // Lightkeeper / caretaker service
    };

    private readonly TradersTable _tradersTable;

    public TraderDirectory(TradersTable tradersTable)
    {
        _tradersTable = tradersTable;
    }

    public IReadOnlyList<AvailableTrader> GetAvailableTraders() => _tradersTable
        .Where(entry => entry.Value?.Base is not null && !ExcludedTraderIds.Contains(entry.Key.ToString()))
        .Select(entry => new AvailableTrader(entry.Key.ToString(), GetDisplayName(entry.Key.ToString(), entry.Value)))
        .OrderBy(trader => trader.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(trader => trader.Id, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public bool TryResolve(TraderDefinition selection, out string traderId)
    {
        traderId = string.Empty;

        if (!string.IsNullOrWhiteSpace(selection.TraderId)
            && !ExcludedTraderIds.Contains(selection.TraderId)
            && TryGetTrader(selection.TraderId, out _))
        {
            traderId = selection.TraderId;
            return true;
        }

        if (TraderMappings.TryGet(selection.Trader, out TraderMapping mapping)
            && !ExcludedTraderIds.Contains(mapping.TraderId)
            && TryGetTrader(mapping.TraderId, out _))
        {
            traderId = mapping.TraderId;
            return true;
        }

        return false;
    }

    public bool TryGetDisplayName(string traderId, out string displayName)
    {
        displayName = string.Empty;
        if (!TryGetTrader(traderId, out Trader? trader) || trader is null)
        {
            return false;
        }

        displayName = GetDisplayName(traderId, trader);
        return true;
    }

    private bool TryGetTrader(string traderId, out Trader? trader)
    {
        trader = null;
        try
        {
            trader = _tradersTable.GetTrader(traderId);
            return trader is not null;
        }
        catch
        {
            return false;
        }
    }

    private static string GetDisplayName(string traderId, Trader trader)
    {
        if (DisplayNameOverrides.TryGetValue(traderId, out string? displayName))
        {
            return displayName;
        }

        if (!string.IsNullOrWhiteSpace(trader.Base.Nickname))
        {
            return trader.Base.Nickname;
        }

        if (!string.IsNullOrWhiteSpace(trader.Base.Name))
        {
            return trader.Base.Name;
        }

        return traderId;
    }
}

public sealed record AvailableTrader(string Id, string DisplayName)
{
    /// <summary>Friendly value written to the definition, without editor-only availability markers.</summary>
    public string ConfigurationName => Id.Equals("579dc571d53a0658a154fbec", StringComparison.OrdinalIgnoreCase)
        ? "Fence"
        : DisplayName;
}
