using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Tests.Core;

public sealed class GraphDisplayFormatterTests
{
    [Fact]
    public void FormatNodeId_WhenIdNotInGraph_ReturnsIdUnchanged()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["a/X"] = new GraphNode { Name = "X", RepoName = "a", Dependencies = [] },
        };

        Assert.Equal("missing/id", GraphDisplayFormatter.FormatNodeId("missing/id", nodes));
    }

    [Fact]
    public void FormatNodeId_WhenNameIsUnique_ReturnsShortName()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["repo/App"] = new GraphNode { Name = "App", RepoName = "repo", Dependencies = [] },
            ["repo/Lib"] = new GraphNode { Name = "Lib", RepoName = "repo", Dependencies = [] },
        };

        Assert.Equal("App", GraphDisplayFormatter.FormatNodeId("repo/App", nodes));
    }

    [Fact]
    public void FormatNodeId_WhenSameNameExistsInMultipleRepos_ReturnsRepoSlashName()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r1/App"] = new GraphNode { Name = "App", RepoName = "r1", Dependencies = [] },
            ["r2/App"] = new GraphNode { Name = "App", RepoName = "r2", Dependencies = [] },
        };

        Assert.Equal("r1/App", GraphDisplayFormatter.FormatNodeId("r1/App", nodes));
        Assert.Equal("r2/App", GraphDisplayFormatter.FormatNodeId("r2/App", nodes));
    }

    [Fact]
    public void FormatNodeId_NameCollision_IsCaseInsensitiveForCounting()
    {
        var nodes = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["r1/app"] = new GraphNode { Name = "app", RepoName = "r1", Dependencies = [] },
            ["r2/App"] = new GraphNode { Name = "App", RepoName = "r2", Dependencies = [] },
        };

        Assert.Equal("r1/app", GraphDisplayFormatter.FormatNodeId("r1/app", nodes));
        Assert.Equal("r2/App", GraphDisplayFormatter.FormatNodeId("r2/App", nodes));
    }
}
