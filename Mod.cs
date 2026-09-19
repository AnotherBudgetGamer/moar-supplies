using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Web;
using SemanticVersion = SemanticVersioning.Version;
using SemanticVersionRange = SemanticVersioning.Range;

namespace MoarSupplies;

/// <summary>
/// Moar Supplies metadata entry point. SPT creates this class before dependency injection is available.
/// </summary>
public sealed class Mod : IModMetadata, IModBlazorMetadata
{
    public string ModGuid { get; init; } = "com.anotherbudgetgamer.moarsupplies";
    public string Name { get; init; } = "Moar Supplies";
    public string Author { get; init; } = "AnotherBudget Gamer";
    public List<string>? Contributors { get; init; } = null;
    public SemanticVersion Version { get; init; } = new("0.5.0");
    public SemanticVersionRange SptVersion { get; init; } = new("~4.1.2");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; } = null;
    public Dictionary<string, SemanticVersionRange>? ModDependencies { get; init; } = null;
    public string? Url { get; init; } = null;
    public string License { get; init; } = "All Rights Reserved";
    public string? WWWRootUrl { get; init; } = "MoarSupplies";
    public string? HomePage { get; init; } = "/moar-supplies";
    public string? HomePageDescription { get; init; } = "View your custom stimulant definitions and their registration status.";
}
