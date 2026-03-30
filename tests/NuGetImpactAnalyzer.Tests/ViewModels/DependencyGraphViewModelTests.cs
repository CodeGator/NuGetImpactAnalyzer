using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;
using NuGetImpactAnalyzer.ViewModels;

namespace NuGetImpactAnalyzer.Tests.ViewModels;

public sealed class DependencyGraphViewModelTests
{
    private sealed class NullApplicationStatus : IApplicationStatus
    {
        public void SetReady(string message = "Ready")
        {
        }

        public void SetBusy(string message)
        {
        }

        public void SetError(string message)
        {
        }
    }

    private sealed class FakeGraphService : IGraphService
    {
        public event EventHandler? GraphChanged;

        public int BuildCallCount { get; private set; }
        public Exception? ThrowOnBuild { get; init; }

        public Task BuildGraphAsync(CancellationToken cancellationToken = default)
        {
            BuildCallCount++;
            if (ThrowOnBuild is not null)
            {
                throw ThrowOnBuild;
            }

            return Task.CompletedTask;
        }

        public void ClearGraph() { }

        public GraphNode? GetNode(string name) => null;

        public IReadOnlyDictionary<string, GraphNode> Nodes { get; } =
            new Dictionary<string, GraphNode> { ["a/b"] = new GraphNode { Name = "b", RepoName = "a", Dependencies = [] } };

        public string FormatGraphText() => "App -> Lib";
    }

    private sealed class NoOpReset : IAnalysisResetService
    {
        public event EventHandler? ResetRequested;
        public void RequestReset() => ResetRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class ListApplicationLog : IApplicationLog
    {
        public List<string> Lines { get; } = new();

        public void AppendLine(string message) => Lines.Add(message);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime NowLocal { get; init; } = new(2026, 3, 28, 14, 30, 0);
    }

    [Fact]
    public async Task BuildGraphCommand_InvokesCoordinatorAndSetsGraphText()
    {
        var graph = new FakeGraphService();
        var coordinator = new GraphBuildCoordinator(graph);
        var log = new ListApplicationLog();
        var clock = new FixedClock();
        var status = new NullApplicationStatus();
        var vm = new DependencyGraphViewModel(coordinator, graph, log, clock, new NoOpReset(), status);

        await ((IAsyncRelayCommand)vm.BuildGraphCommand).ExecuteAsync(null);

        Assert.Equal(1, graph.BuildCallCount);
        Assert.Equal("App -> Lib", vm.GraphText);
        Assert.Single(log.Lines);
        Assert.Contains("Graph built:", log.Lines[0], StringComparison.Ordinal);
        Assert.Contains("1 node(s)", log.Lines[0], StringComparison.Ordinal);
        Assert.StartsWith("[14:30:00]", log.Lines[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildGraphCommand_OnException_SetsErrorTextAndLogs()
    {
        var graph = new FakeGraphService { ThrowOnBuild = new InvalidOperationException("bad") };
        var coordinator = new GraphBuildCoordinator(graph);
        var log = new ListApplicationLog();
        var clock = new FixedClock();
        var status = new NullApplicationStatus();
        var vm = new DependencyGraphViewModel(coordinator, graph, log, clock, new NoOpReset(), status);

        await ((IAsyncRelayCommand)vm.BuildGraphCommand).ExecuteAsync(null);

        Assert.Contains("bad", vm.GraphText, StringComparison.Ordinal);
        Assert.Single(log.Lines);
        Assert.Contains("Graph build failed", log.Lines[0], StringComparison.Ordinal);
    }
}
