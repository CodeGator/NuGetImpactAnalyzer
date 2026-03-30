using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Dependency graph panel: triggers graph build via <see cref="IGraphBuildCoordinator"/> and formatted text.
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
        };
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
                _status.SetReady("Graph build finished.");
            }
            else
            {
                _status.SetError(result.GraphText);
            }
        }
        catch (Exception ex)
        {
            GraphText = $"Could not build graph: {ex.Message}";
            _status.SetError($"Graph build failed: {ex.Message}");
            _log.AppendTimestampedLine(_clock, $"Graph build failed: {ex.Message}");
        }
        finally
        {
            IsBuildingGraph = false;
        }
    }
}
