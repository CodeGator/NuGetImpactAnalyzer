using System.Collections.ObjectModel;
using CodeGator.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Dependency graph panel: triggers graph build via <see cref="IGraphBuildCoordinator"/>, formatted text,
/// and a <see cref="CgDiagram"/> data model (<see cref="DiagramNodes"/> / <see cref="DiagramEdges"/>).
/// </summary>
public partial class DependencyGraphViewModel : ObservableObject
{
    private readonly IGraphBuildCoordinator _graphBuild;
    private readonly IApplicationLog _log;
    private readonly IClock _clock;
    private readonly IApplicationStatus _status;
    private readonly IGraphService _graphService;

    [ObservableProperty]
    private string _graphText = DependencyGraphPresentation.InitialHint;

    [ObservableProperty]
    private bool _isBuildingGraph;

    /// <summary>Nodes for the Analyzer Graph tab <c>CgDiagram</c> (same graph as <see cref="GraphText"/>).</summary>
    public ObservableCollection<CgDiagramNode> DiagramNodes { get; } = new();

    /// <summary>Edges for the Analyzer Graph tab <c>CgDiagram</c>.</summary>
    public ObservableCollection<CgDiagramEdge> DiagramEdges { get; } = new();

    public DependencyGraphViewModel(
        IGraphBuildCoordinator graphBuild,
        IGraphService graphService,
        IApplicationLog log,
        IClock clock,
        IAnalysisResetService reset,
        IApplicationStatus status)
    {
        _graphBuild = graphBuild;
        _graphService = graphService;
        _log = log;
        _clock = clock;
        _status = status;
        reset.ResetRequested += (_, _) =>
        {
            _graphService.ClearGraph();
            GraphText = DependencyGraphPresentation.InitialHint;
            ClearDiagram();
        };
    }

    private void ClearDiagram()
    {
        DiagramNodes.Clear();
        DiagramEdges.Clear();
    }

    private void RebuildDiagramFromGraph()
    {
        ClearDiagram();
        var nodes = _graphService.Nodes;
        if (nodes.Count == 0)
        {
            return;
        }

        foreach (var kv in nodes.OrderBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            var id = kv.Key;
            var n = kv.Value;
            var label = GraphDisplayFormatter.FormatNodeId(id, nodes);
            DiagramNodes.Add(new CgDiagramNode(id, label, n.RepoName) { SwimlaneId = n.RepoName });
        }

        var seenEdges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in nodes)
        {
            var fromId = kv.Key;
            foreach (var depId in kv.Value.Dependencies)
            {
                if (!nodes.ContainsKey(depId))
                {
                    continue;
                }

                var edgeKey = fromId + "\u001f" + depId;
                if (!seenEdges.Add(edgeKey))
                {
                    continue;
                }

                var edgeLabel = string.Empty;
                if (kv.Value.PackageDependencyConstraints.TryGetValue(depId, out var constraint)
                    && !string.IsNullOrWhiteSpace(constraint))
                {
                    edgeLabel = constraint;
                }

                DiagramEdges.Add(new CgDiagramEdge(fromId, depId, edgeLabel));
            }
        }
    }

    [RelayCommand]
    private async Task BuildGraphAsync()
    {
        IsBuildingGraph = true;
        _status.SetBusy("Building dependency graph…");
        GraphText = DependencyGraphPresentation.BuildingPlaceholder;
        try
        {
            var result = await _graphBuild.BuildAsync(CancellationToken.None).ConfigureAwait(true);
            GraphText = result.GraphText;
            _log.AppendTimestampedLine(_clock, result.LogDetailLine);
            if (result.Success)
            {
                RebuildDiagramFromGraph();
                _status.SetReady("Graph build finished.");
            }
            else
            {
                ClearDiagram();
                _status.SetError(result.GraphText);
            }
        }
        catch (Exception ex)
        {
            GraphText = $"Could not build graph: {ex.Message}";
            ClearDiagram();
            _status.SetError($"Graph build failed: {ex.Message}");
            _log.AppendTimestampedLine(_clock, $"Graph build failed: {ex.Message}");
        }
        finally
        {
            IsBuildingGraph = false;
        }
    }
}
