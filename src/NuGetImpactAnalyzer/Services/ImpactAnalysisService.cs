using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class ImpactAnalysisService : IImpactAnalysisService
{
    private readonly IGraphService _graphService;

    public ImpactAnalysisService(IGraphService graphService)
    {
        _graphService = graphService;
    }

    /// <inheritdoc />
    public IReadOnlyList<ImpactedPackageResult> AnalyzeImpact(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return [];
        }

        return AnalyzeImpactCore(_graphService.Nodes, packageName.Trim());
    }

    private static IReadOnlyList<ImpactedPackageResult> AnalyzeImpactCore(
        IReadOnlyDictionary<string, GraphNode> nodes,
        string trimmedPackageQuery)
    {
        if (nodes.Count == 0)
        {
            return [];
        }

        var startIds = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, trimmedPackageQuery);
        if (startIds.Count == 0)
        {
            return [];
        }

        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var impactedIds = DependencyGraphTraversal.CollectTransitiveDependentsOnly(startIds, reverse);

        var results = new List<ImpactedPackageResult>();
        foreach (var id in impactedIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var display = GraphDisplayFormatter.FormatNodeId(id, nodes);
            var definite = startIds.Any(s => ImpactPathAnalyzer.ExistsDefinitePath(nodes, id, s));
            results.Add(new ImpactedPackageResult
            {
                NodeId = id,
                DisplayLabel = display,
                Severity = definite ? ImpactSeverity.Definite : ImpactSeverity.Possible,
            });
        }

        return results;
    }
}
