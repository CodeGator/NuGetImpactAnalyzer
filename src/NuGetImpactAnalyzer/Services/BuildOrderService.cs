using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class BuildOrderService : IBuildOrderService
{
    private readonly IGraphService _graphService;

    public BuildOrderService(IGraphService graphService)
    {
        _graphService = graphService;
    }

    /// <inheritdoc />
    public BuildOrderResult GetBuildOrder(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return new BuildOrderResult(false, [], "Enter a package or project name.");
        }

        var nodes = _graphService.Nodes;
        if (nodes.Count == 0)
        {
            return new BuildOrderResult(false, [], "The dependency graph is empty. Build the graph first.");
        }

        var trimmed = packageName.Trim();
        var startIds = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, trimmed);
        if (startIds.Count == 0)
        {
            return new BuildOrderResult(false, [], "No matching package or project in the graph.");
        }

        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var closure = DependencyGraphTraversal.CollectDownstreamClosure(startIds, reverse);
        var topo = DependencyGraphTraversal.TopologicalSort(closure, nodes, reverse);
        if (!topo.Success)
        {
            return new BuildOrderResult(false, [], topo.ErrorMessage!);
        }

        var display = topo.Order!
            .Select(id => GraphDisplayFormatter.FormatNodeId(id, nodes))
            .ToList();

        return new BuildOrderResult(true, display, null);
    }
}
