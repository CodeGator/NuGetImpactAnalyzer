using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class BuildOrderServiceTests
{
    private sealed class FakeGraphService : IGraphService
    {
        public event EventHandler? GraphChanged;

        public required Dictionary<string, GraphNode> NodeMap { get; init; }

        public IReadOnlyDictionary<string, GraphNode> Nodes => NodeMap;

        public Task BuildGraphAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void ClearGraph() { }

        public GraphNode? GetNode(string name) => null;

        public string FormatGraphText() => string.Empty;
    }

    [Fact]
    public void GetBuildOrder_Chain_DependenciesBeforeDependents()
    {
        // D <- B <- A  (A depends on B, B depends on D)
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("D");

        Assert.True(result.Success);
        Assert.Equal(["D", "B", "A"], result.OrderedPackages);
    }

    [Fact]
    public void GetBuildOrder_Diamond_DependenciesBeforeSharedRoot()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
            ["r/C"] = new GraphNode { Name = "C", RepoName = "r", Dependencies = ["r/D"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B", "r/C"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("D");

        Assert.True(result.Success);
        Assert.Equal(4, result.OrderedPackages.Count);
        Assert.Equal("D", result.OrderedPackages[0]);
        Assert.Equal("A", result.OrderedPackages[3]);
        var order = result.OrderedPackages.ToList();
        var idxB = order.IndexOf("B");
        var idxC = order.IndexOf("C");
        Assert.True(idxB is > 0 and < 3);
        Assert.True(idxC is > 0 and < 3);
    }

    [Fact]
    public void GetBuildOrder_WhenCycleInClosure_ReturnsError()
    {
        // A -> B -> A within closure (both depend on each other)
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/A"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("A");

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("cycle", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuildOrder_WhenWhitespace_ReturnsError()
    {
        var graph = new FakeGraphService { NodeMap = [] };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("   ");

        Assert.False(result.Success);
    }

    [Fact]
    public void GetBuildOrder_WhenNull_ReturnsValidationError()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = [] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder(null!);

        Assert.False(result.Success);
        Assert.Contains("Enter a package", result.ErrorMessage ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public void GetBuildOrder_TrimsPackageNameBeforeResolve()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("  D  ");

        Assert.True(result.Success);
        Assert.Equal(["D", "B"], result.OrderedPackages);
    }

    [Fact]
    public void GetBuildOrder_WhenGraphEmpty_ReturnsError()
    {
        var graph = new FakeGraphService { NodeMap = [] };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("X");

        Assert.False(result.Success);
        Assert.Contains("empty", result.ErrorMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuildOrder_WhenShortNameMatchesMultipleNodes_IncludesAllDownstreamWithDependenciesFirst()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r1/Lib"] = new GraphNode { Name = "Lib", RepoName = "r1", Dependencies = [] },
            ["r2/Lib"] = new GraphNode { Name = "Lib", RepoName = "r2", Dependencies = [] },
            ["r1/App"] = new GraphNode { Name = "App", RepoName = "r1", Dependencies = ["r1/Lib"] },
            ["r2/App"] = new GraphNode { Name = "App", RepoName = "r2", Dependencies = ["r2/Lib"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("Lib");

        Assert.True(result.Success);
        Assert.Equal(4, result.OrderedPackages.Count);
        var order = result.OrderedPackages.ToList();
        var idxR1Lib = order.IndexOf("r1/Lib");
        var idxR2Lib = order.IndexOf("r2/Lib");
        var idxR1App = order.IndexOf("r1/App");
        var idxR2App = order.IndexOf("r2/App");
        Assert.True(idxR1Lib >= 0 && idxR1Lib < idxR1App);
        Assert.True(idxR2Lib >= 0 && idxR2Lib < idxR2App);
    }

    [Fact]
    public void GetBuildOrder_WhenNoMatch_ReturnsError()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = [] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new BuildOrderService(graph);

        var result = sut.GetBuildOrder("Unknown");

        Assert.False(result.Success);
        Assert.Contains("No matching", result.ErrorMessage ?? "", StringComparison.Ordinal);
    }
}
