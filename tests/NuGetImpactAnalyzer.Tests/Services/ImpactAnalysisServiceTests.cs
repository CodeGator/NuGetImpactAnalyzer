using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class ImpactAnalysisServiceTests
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
    public void AnalyzeImpact_ReturnsTransitiveDependents_WithDefiniteWhenOnlyProjectLikeEdges()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/C"] = new GraphNode { Name = "C", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B", "r/C"] },
        };

        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var results = sut.AnalyzeImpact("D");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.DisplayLabel.Equals("B", StringComparison.OrdinalIgnoreCase) && r.Severity == ImpactSeverity.Definite);
        Assert.Contains(results, r => r.DisplayLabel.Equals("A", StringComparison.OrdinalIgnoreCase) && r.Severity == ImpactSeverity.Definite);
    }

    [Fact]
    public void AnalyzeImpact_WhenUnknown_ReturnsEmpty()
    {
        var graph = new FakeGraphService { NodeMap = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase) };
        var sut = new ImpactAnalysisService(graph);

        Assert.Empty(sut.AnalyzeImpact("Nothing"));
    }

    [Fact]
    public void AnalyzeImpact_WhenNoDependents_ReturnsEmpty()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = [] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        Assert.Empty(sut.AnalyzeImpact("A"));
    }

    [Fact]
    public void AnalyzeImpact_WhenTargetIsNull_ReturnsEmpty()
    {
        var graph = new FakeGraphService
        {
            NodeMap = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["r/X"] = new GraphNode { Name = "X", RepoName = "r", Dependencies = [] },
            },
        };
        var sut = new ImpactAnalysisService(graph);

        Assert.Empty(sut.AnalyzeImpact(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnalyzeImpact_WhenTargetIsEmptyOrWhitespace_ReturnsEmpty(string packageName)
    {
        var graph = new FakeGraphService
        {
            NodeMap = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
            {
                ["r/X"] = new GraphNode { Name = "X", RepoName = "r", Dependencies = [] },
            },
        };
        var sut = new ImpactAnalysisService(graph);

        Assert.Empty(sut.AnalyzeImpact(packageName));
    }

    [Fact]
    public void AnalyzeImpact_WhenGraphIsEmpty_ReturnsEmpty()
    {
        var graph = new FakeGraphService { NodeMap = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase) };
        var sut = new ImpactAnalysisService(graph);

        Assert.Empty(sut.AnalyzeImpact("Anything"));
    }

    [Fact]
    public void AnalyzeImpact_MatchesByFullNodeIdOrShortName()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = new GraphNode { Name = "Lib", RepoName = "r", Dependencies = [] },
            ["r/App"] = new GraphNode { Name = "App", RepoName = "r", Dependencies = ["r/Lib"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var byId = sut.AnalyzeImpact("r/Lib");
        var byName = sut.AnalyzeImpact("lib");

        Assert.Equal(byId.Count, byName.Count);
        Assert.Single(byId);
        Assert.Equal("App", byId[0].DisplayLabel);
        Assert.Equal(ImpactSeverity.Definite, byId[0].Severity);
    }

    [Fact]
    public void AnalyzeImpact_DisambiguatesDuplicateShortNamesViaGraphDisplayFormatter()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r1/Lib"] = new GraphNode { Name = "Lib", RepoName = "r1", Dependencies = [] },
            ["r2/Lib"] = new GraphNode { Name = "Lib", RepoName = "r2", Dependencies = [] },
            ["r1/App"] = new GraphNode { Name = "App", RepoName = "r1", Dependencies = ["r1/Lib"] },
            ["r2/App"] = new GraphNode { Name = "App", RepoName = "r2", Dependencies = ["r2/Lib"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var results = sut.AnalyzeImpact("r1/Lib");

        Assert.Single(results);
        Assert.Equal("r1/App", results[0].DisplayLabel);
        Assert.Equal(ImpactSeverity.Definite, results[0].Severity);
    }

    [Fact]
    public void AnalyzeImpact_TrimsPackageNameBeforeResolve()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = new GraphNode { Name = "Lib", RepoName = "r", Dependencies = [] },
            ["r/App"] = new GraphNode { Name = "App", RepoName = "r", Dependencies = ["r/Lib"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var results = sut.AnalyzeImpact("  Lib  ");

        Assert.Single(results);
        Assert.Equal("App", results[0].DisplayLabel);
    }

    [Fact]
    public void AnalyzeImpact_WhenShortNameMatchesMultipleNodes_CollectsAllDependents()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r1/Lib"] = new GraphNode { Name = "Lib", RepoName = "r1", Dependencies = [] },
            ["r2/Lib"] = new GraphNode { Name = "Lib", RepoName = "r2", Dependencies = [] },
            ["r1/App"] = new GraphNode { Name = "App", RepoName = "r1", Dependencies = ["r1/Lib"] },
            ["r2/App"] = new GraphNode { Name = "App", RepoName = "r2", Dependencies = ["r2/Lib"] },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var results = sut.AnalyzeImpact("Lib");

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.DisplayLabel == "r1/App");
        Assert.Contains(results, r => r.DisplayLabel == "r2/App");
    }

    [Fact]
    public void AnalyzeImpact_WhenPackageRangeNotSatisfied_MarksPossible()
    {
        var pkgConstraints = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = "[1.0.0, 1.5.0]",
        };
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = new GraphNode
            {
                Name = "Lib",
                RepoName = "r",
                Dependencies = [],
                ResolvedPackageVersion = "2.0.0",
            },
            ["r/App"] = new GraphNode
            {
                Name = "App",
                RepoName = "r",
                Dependencies = ["r/Lib"],
                PackageDependencyConstraints = pkgConstraints,
            },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var results = sut.AnalyzeImpact("Lib");

        Assert.Single(results);
        Assert.Equal(ImpactSeverity.Possible, results[0].Severity);
    }

    [Fact]
    public void AnalyzeImpact_WhenPackageRangeSatisfied_MarksDefinite()
    {
        var pkgConstraints = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = "[1.0.0, 3.0.0)",
        };
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = new GraphNode
            {
                Name = "Lib",
                RepoName = "r",
                Dependencies = [],
                ResolvedPackageVersion = "1.5.0",
            },
            ["r/App"] = new GraphNode
            {
                Name = "App",
                RepoName = "r",
                Dependencies = ["r/Lib"],
                PackageDependencyConstraints = pkgConstraints,
            },
        };
        var graph = new FakeGraphService { NodeMap = nodes };
        var sut = new ImpactAnalysisService(graph);

        var results = sut.AnalyzeImpact("Lib");

        Assert.Single(results);
        Assert.Equal(ImpactSeverity.Definite, results[0].Severity);
    }

}
