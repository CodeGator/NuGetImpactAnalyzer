namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// User-visible strings for the impact analysis panel (view concerns, not business rules).
/// </summary>
public static class ImpactAnalysisPresentation
{
    /// <summary>Initial helper text shown before running impact analysis.</summary>
    public const string InitialHint =
        "Enter a package or project name, then analyze. Build the graph first.";

    /// <summary>Hint shown when analysis returns no impacted packages.</summary>
    public const string NoMatchesHint =
        "No impacted packages found. Build the graph, then use a name that exists in the graph.";

    /// <summary>Hint describing how the build order list is computed.</summary>
    public const string BuildOrderInitialHint =
        "Uses the target and all transitive dependents. Dependencies are listed before dependents.";

    /// <summary>Formats the impact summary hint shown above the results list.</summary>
    /// <param name="definiteCount">Number of definite impacts.</param>
    /// <param name="possibleCount">Number of possible impacts.</param>
    public static string FormatImpactSummaryHint(int definiteCount, int possibleCount) =>
        definiteCount == 0 && possibleCount == 0
            ? NoMatchesHint
            : $"{definiteCount} definite, {possibleCount} possible (transitive dependents).";

    /// <summary>Formats the hint shown after successfully computing build order.</summary>
    /// <param name="stepCount">Number of build steps.</param>
    public static string FormatBuildOrderSuccessHint(int stepCount) =>
        $"{stepCount} step(s). Build dependencies first (top to bottom).";

    /// <summary>
    /// Formats build order items as numbered lines (1-based).
    /// </summary>
    /// <param name="orderedDisplayNames">Ordered item labels.</param>
    public static IReadOnlyList<string> FormatNumberedBuildOrderLines(IReadOnlyList<string> orderedDisplayNames)
    {
        var lines = new List<string>(orderedDisplayNames.Count);
        for (var i = 0; i < orderedDisplayNames.Count; i++)
        {
            lines.Add($"{i + 1}. {orderedDisplayNames[i]}");
        }

        return lines;
    }
}
