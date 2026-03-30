using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class DependencyGraphTraversalTests
{
    [Fact]
    public void ResolveMatchingNodeIds_MatchesFullNodeId_CaseInsensitive()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["Repo/MyApp"] = new GraphNode { Name = "MyApp", RepoName = "Repo", Dependencies = [] },
        };

        var a = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "repo/myapp");
        var b = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "Repo/MyApp");

        Assert.Equal("Repo/MyApp", Assert.Single(a));
        Assert.Equal("Repo/MyApp", Assert.Single(b));
    }

    [Fact]
    public void ResolveMatchingNodeIds_MatchesShortName_CaseInsensitive()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/X"] = new GraphNode { Name = "Shared", RepoName = "r", Dependencies = [] },
        };

        var ids = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "shared");

        Assert.Equal("r/X", Assert.Single(ids));
    }

    [Fact]
    public void ResolveMatchingNodeIds_WhenDuplicateShortNames_ReturnsAllNodeIds()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r1/Lib"] = new GraphNode { Name = "Lib", RepoName = "r1", Dependencies = [] },
            ["r2/Lib"] = new GraphNode { Name = "Lib", RepoName = "r2", Dependencies = [] },
        };

        var ids = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "Lib");

        Assert.Equal(2, ids.Count);
        Assert.Contains("r1/Lib", ids);
        Assert.Contains("r2/Lib", ids);
    }

    [Fact]
    public void ResolveMatchingNodeIds_WhenNoMatch_ReturnsEmpty()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = [] },
        };

        Assert.Empty(DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "Missing"));
    }

    [Fact]
    public void BuildReverseAdjacency_MapsEachDependencyToItsDependents()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/L"] = new GraphNode { Name = "L", RepoName = "r", Dependencies = [] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/L"] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/L"] },
        };

        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);

        Assert.True(reverse.TryGetValue("r/L", out var deps));
        Assert.Equal(2, deps!.Count);
        Assert.Contains("r/A", deps);
        Assert.Contains("r/B", deps);
    }

    [Fact]
    public void TopologicalSort_SingleVertex_Succeeds()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Only"] = new GraphNode { Name = "Only", RepoName = "r", Dependencies = [] },
        };
        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var closure = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "r/Only" };

        var result = DependencyGraphTraversal.TopologicalSort(closure, nodes, reverse);

        Assert.True(result.Success);
        Assert.Equal(["r/Only"], result.Order);
    }

    [Fact]
    public void CollectTransitiveDependentsOnly_ExcludesStarts()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
        };
        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var starts = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "D");

        var impacted = DependencyGraphTraversal.CollectTransitiveDependentsOnly(starts, reverse);

        Assert.Equal(2, impacted.Count);
        Assert.Contains("r/B", impacted);
        Assert.Contains("r/A", impacted);
        Assert.DoesNotContain("r/D", impacted);
    }

    [Fact]
    public void CollectDownstreamClosure_IncludesStartsAndDependents()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
        };
        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var starts = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "D");

        var closure = DependencyGraphTraversal.CollectDownstreamClosure(starts, reverse);

        Assert.Equal(2, closure.Count);
        Assert.Contains("r/D", closure);
        Assert.Contains("r/B", closure);
    }

    [Fact]
    public void TopologicalSort_Chain_ReturnsDependencyFirstOrder()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
        };
        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var starts = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "D");
        var closure = DependencyGraphTraversal.CollectDownstreamClosure(starts, reverse);

        var result = DependencyGraphTraversal.TopologicalSort(closure, nodes, reverse);

        Assert.True(result.Success);
        Assert.NotNull(result.Order);
        Assert.Equal(["r/D", "r/B", "r/A"], result.Order);
    }

    [Fact]
    public void TopologicalSort_Cycle_ReturnsFailure()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/A"] },
        };
        var reverse = DependencyGraphTraversal.BuildReverseAdjacency(nodes);
        var starts = DependencyGraphTraversal.ResolveMatchingNodeIds(nodes, "A");
        var closure = DependencyGraphTraversal.CollectDownstreamClosure(starts, reverse);

        var result = DependencyGraphTraversal.TopologicalSort(closure, nodes, reverse);

        Assert.False(result.Success);
        Assert.Contains("cycle", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
