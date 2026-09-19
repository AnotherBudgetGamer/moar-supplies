namespace MoarSupplies.Models;

/// <summary>
/// Stable internal identifiers derived from a user-facing stim ID.
/// </summary>
public sealed record StimRegistrationIds(
    string ItemTemplateId,
    string BuffKey,
    string TraderAssortId);
