using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Consistent display labels for graph node ids (matches dependency graph text).
/// </summary>
public static class GraphDisplayFormatter
{
    public static string FormatNodeId(string nodeId, IReadOnlyDictionary<string, GraphNode> nodes)
    {
        if (!nodes.TryGetValue(nodeId, out var node))
        {
            return nodeId;
        }

        var nameCount = nodes.Values
            .GroupBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return nameCount[node.Name] > 1 ? $"{node.RepoName}/{node.Name}" : node.Name;
    }
}
