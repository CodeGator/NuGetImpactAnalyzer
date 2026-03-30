using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Determines whether a transitive dependent has a path to the target where every package edge
/// has a satisfiable required version vs the dependency's resolved version.
/// </summary>
public static class ImpactPathAnalyzer
{
    /// <summary>
    /// True if there is a path from <paramref name="current"/> toward dependencies that reaches
    /// <paramref name="targetDependencyId"/>, where every <see cref="GraphNode.PackageDependencyConstraints"/>
    /// edge on the path satisfies <see cref="PackageVersionSatisfaction.IsSatisfied"/>.
    /// Project-reference edges (no constraint entry) are always treated as definite hops.
    /// </summary>
    public static bool ExistsDefinitePath(
        IReadOnlyDictionary<string, GraphNode> nodes,
        string current,
        string targetDependencyId)
    {
        foreach (var dep in nodes[current].Dependencies)
        {
            if (!CanReachDependency(nodes, dep, targetDependencyId))
            {
                continue;
            }

            if (!IsDefiniteEdge(nodes, current, dep))
            {
                continue;
            }

            if (string.Equals(dep, targetDependencyId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ExistsDefinitePath(nodes, dep, targetDependencyId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsDefiniteEdge(
        IReadOnlyDictionary<string, GraphNode> nodes,
        string dependentId,
        string dependencyId)
    {
        if (!nodes[dependentId].PackageDependencyConstraints.TryGetValue(dependencyId, out var required))
        {
            return true;
        }

        return PackageVersionSatisfaction.IsSatisfied(required, nodes[dependencyId].ResolvedPackageVersion);
    }

    /// <summary>
    /// Whether <paramref name="from"/> transitively depends on <paramref name="target"/> (follows <see cref="GraphNode.Dependencies"/>).
    /// </summary>
    public static bool CanReachDependency(
        IReadOnlyDictionary<string, GraphNode> nodes,
        string from,
        string target)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return Dfs(from);

        bool Dfs(string u)
        {
            if (string.Equals(u, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!visited.Add(u))
            {
                return false;
            }

            foreach (var d in nodes[u].Dependencies)
            {
                if (Dfs(d))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
