using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class GraphBuildCoordinatorTests
{
    private sealed class FakeGraphService : IGraphService
    {
        public event EventHandler? GraphChanged;

        public Exception? ThrowOnBuild { get; init; }

        public Func<CancellationToken, Task>? BuildAsyncImpl { get; init; }

        public Task BuildGraphAsync(CancellationToken cancellationToken = default)
        {
            if (BuildAsyncImpl is not null)
            {
                return BuildAsyncImpl(cancellationToken);
            }

            if (ThrowOnBuild is not null)
            {
                throw ThrowOnBuild;
            }

            return Task.CompletedTask;
        }

        public void ClearGraph() { }

        public GraphNode? GetNode(string name) => null;

        public IReadOnlyDictionary<string, GraphNode> Nodes { get; init; } =
            new Dictionary<string, GraphNode> { ["x/y"] = new GraphNode { Name = "y", RepoName = "x", Dependencies = [] } };

        public static FakeGraphService WithEmptyNodes() =>
            new()
            {
                Nodes = new Dictionary<string, GraphNode>(),
            };

        public string FormatGraphText() => "A -> B";
    }

    [Fact]
    public async Task BuildAsync_OnSuccess_ReturnsFormattedTextAndSummaryLine()
    {
        var graph = new FakeGraphService();
        var sut = new GraphBuildCoordinator(graph);

        var result = await sut.BuildAsync();

        Assert.True(result.Success);
        Assert.Equal("A -> B", result.GraphText);
        Assert.Contains("Graph built:", result.LogDetailLine, StringComparison.Ordinal);
        Assert.Contains("1 node(s)", result.LogDetailLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_OnSuccess_WithZeroNodes_LogsZeroInSummaryLine()
    {
        var graph = FakeGraphService.WithEmptyNodes();
        var sut = new GraphBuildCoordinator(graph);

        var result = await sut.BuildAsync();

        Assert.True(result.Success);
        Assert.Contains("0 node(s)", result.LogDetailLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_OnFailure_ReturnsErrorGraphTextAndLogLine()
    {
        var graph = new FakeGraphService { ThrowOnBuild = new InvalidOperationException("nope") };
        var sut = new GraphBuildCoordinator(graph);

        var result = await sut.BuildAsync();

        Assert.False(result.Success);
        Assert.Contains("nope", result.GraphText, StringComparison.Ordinal);
        Assert.Contains("Graph build failed", result.LogDetailLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_WhenBuildThrowsOperationCanceled_ReturnsFailure()
    {
        var graph = new FakeGraphService { ThrowOnBuild = new OperationCanceledException() };
        var sut = new GraphBuildCoordinator(graph);

        var result = await sut.BuildAsync();

        Assert.False(result.Success);
        Assert.Contains("Graph build failed", result.LogDetailLine, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_PassesCancellationTokenToGraphService()
    {
        CancellationToken? seen = null;
        var graph = new FakeGraphService
        {
            BuildAsyncImpl = token =>
            {
                seen = token;
                return Task.CompletedTask;
            },
        };
        var sut = new GraphBuildCoordinator(graph);
        using var cts = new CancellationTokenSource();

        await sut.BuildAsync(cts.Token);

        Assert.True(seen.HasValue);
        Assert.Equal(cts.Token, seen.Value);
    }
}
