namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Computes a build order (dependencies before dependents) for the subgraph induced by a target
/// and its transitive dependents, using a topological sort. Fails if that subgraph contains a cycle.
/// </summary>
public interface IBuildOrderService
{
    /// <summary>
    /// Returns a topologically sorted list of display names, or an error if the target is missing,
    /// the graph is empty, or a cycle is detected.
    /// </summary>
    BuildOrderResult GetBuildOrder(string packageName);
}
