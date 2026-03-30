using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class ImpactPathAnalyzerTests
{
    [Fact]
    public void CanReachDependency_ReturnsTrueWhenTargetIsReachable()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/C"] = new GraphNode { Name = "C", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/C"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
        };

        Assert.True(ImpactPathAnalyzer.CanReachDependency(nodes, "r/A", "r/C"));
    }

    [Fact]
    public void CanReachDependency_ReturnsFalseWhenNotReachable()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/X"] = new GraphNode { Name = "X", RepoName = "r", Dependencies = [] },
            ["r/Y"] = new GraphNode { Name = "Y", RepoName = "r", Dependencies = [] },
        };

        Assert.False(ImpactPathAnalyzer.CanReachDependency(nodes, "r/X", "r/Y"));
    }

    [Fact]
    public void ExistsDefinitePath_TrueWhenOnlyProjectReferenceEdges()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/D"] = new GraphNode { Name = "D", RepoName = "r", Dependencies = [] },
            ["r/B"] = new GraphNode { Name = "B", RepoName = "r", Dependencies = ["r/D"] },
            ["r/A"] = new GraphNode { Name = "A", RepoName = "r", Dependencies = ["r/B"] },
        };

        Assert.True(ImpactPathAnalyzer.ExistsDefinitePath(nodes, "r/A", "r/D"));
    }

    [Fact]
    public void ExistsDefinitePath_FalseWhenPackageConstraintNotSatisfied()
    {
        var pkg = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["r/Lib"] = "[1.0, 1.5]" };
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
                PackageDependencyConstraints = pkg,
            },
        };

        Assert.False(ImpactPathAnalyzer.ExistsDefinitePath(nodes, "r/App", "r/Lib"));
    }

    [Fact]
    public void ExistsDefinitePath_TrueWhenPackageConstraintSatisfied()
    {
        var pkg = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) { ["r/Lib"] = "[1.0, 3.0)" };
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r/Lib"] = new GraphNode
            {
                Name = "Lib",
                RepoName = "r",
                Dependencies = [],
                ResolvedPackageVersion = "1.2.0",
            },
            ["r/App"] = new GraphNode
            {
                Name = "App",
                RepoName = "r",
                Dependencies = ["r/Lib"],
                PackageDependencyConstraints = pkg,
            },
        };

        Assert.True(ImpactPathAnalyzer.ExistsDefinitePath(nodes, "r/App", "r/Lib"));
    }
}
