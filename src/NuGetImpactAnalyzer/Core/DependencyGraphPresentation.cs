namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Placeholder copy for the dependency graph panel (kept out of view models as literals).
/// </summary>
public static class DependencyGraphPresentation
{
    /// <summary>Initial helper text shown before any graph build.</summary>
    public const string InitialHint =
        "Click \"Build Graph\" to analyze project and package links across configured repos.";

    /// <summary>Temporary placeholder shown while graph building is in progress.</summary>
    public const string BuildingPlaceholder = "Building…";
}
