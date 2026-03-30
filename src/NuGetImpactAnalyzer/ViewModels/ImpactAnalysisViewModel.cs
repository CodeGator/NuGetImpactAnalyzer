using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Impact tab shell: target-based impact analysis; composes simulation and build-order child view models.
/// </summary>
public partial class ImpactAnalysisViewModel : ObservableObject, IDisposable
{
    private readonly IImpactAnalysisInteractionService _impactInteraction;
    private readonly IGraphService _graphService;
    private readonly IAppConfigurationService _appConfiguration;
    private readonly IImpactTargetPreferencesService _targetPreferences;
    private readonly IAnalysisResetService _reset;
    private readonly IApplicationStatus _status;
    private readonly IApplicationLog _log;
    private readonly IClock _clock;

    public ImpactBuildOrderViewModel BuildOrder { get; }

    public ObservableCollection<ImpactedPackageResult> ImpactedPackages { get; } = new();

    /// <summary>Graph node ids (repo/project) after the last successful build; also type to use a short name when unique.</summary>
    public ObservableCollection<string> TargetPackageChoices { get; } = new();

    [ObservableProperty]
    private string _targetPackage = string.Empty;

    [ObservableProperty]
    private string _impactHint = ImpactAnalysisPresentation.InitialHint;

    [ObservableProperty]
    private bool _isAnalyzingImpact;

    [ObservableProperty]
    private bool _isGraphBuilt;

    public bool CanAnalyze =>
        IsGraphBuilt
        && !IsAnalyzingImpact
        && !string.IsNullOrWhiteSpace(TargetPackage);

    partial void OnTargetPackageChanged(string value) => OnPropertyChanged(nameof(CanAnalyze));

    partial void OnIsAnalyzingImpactChanged(bool value) => OnPropertyChanged(nameof(CanAnalyze));

    partial void OnIsGraphBuiltChanged(bool value) => OnPropertyChanged(nameof(CanAnalyze));

    public ImpactAnalysisViewModel(
        IImpactAnalysisInteractionService impactInteraction,
        IGraphService graphService,
        IAppConfigurationService appConfiguration,
        ImpactBuildOrderViewModel buildOrder,
        IImpactTargetPreferencesService targetPreferences,
        IAnalysisResetService reset,
        IApplicationStatus status,
        IApplicationLog log,
        IClock clock)
    {
        _impactInteraction = impactInteraction;
        _graphService = graphService;
        _appConfiguration = appConfiguration;
        BuildOrder = buildOrder;
        _targetPreferences = targetPreferences;
        _reset = reset;
        _status = status;
        _log = log;
        _clock = clock;

        _graphService.GraphChanged += OnGraphChanged;
        _reset.ResetRequested += OnResetRequested;
        RefreshGraphDerivedChoices();

        var last = _targetPreferences.LoadLastTargetPackage();
        if (!string.IsNullOrWhiteSpace(last))
        {
            TargetPackage = last;
        }

        CoerceTargetPackageToCurrentGraph();
    }

    private void OnGraphChanged(object? sender, EventArgs e) => PostToUi(RefreshGraphDerivedChoices);

    private void RefreshGraphDerivedChoices()
    {
        var nodes = _graphService.Nodes;
        var configuredRepos = GetConfiguredRepoNameSet();

        IsGraphBuilt = nodes.Count > 0;

        var orderedIds = nodes
            .Where(kv => configuredRepos.Contains(kv.Value.RepoName))
            .Select(kv => kv.Key)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
        TargetPackageChoices.ReplaceAll(orderedIds);

        CoerceTargetPackageToCurrentGraph();
    }

    /// <summary>Names of repos in the saved config — used so dropdowns match Settings even if the in-memory graph is stale after removing a repo.</summary>
    private HashSet<string> GetConfiguredRepoNameSet() =>
        _appConfiguration.Load().Config.Repos
            .Select(r => r.Name.Trim())
            .Where(n => n.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Clears the target when it no longer resolves in the current graph (e.g. persisted id from a removed repo/project).
    /// Skips when the graph is empty so a saved value survives until the first successful Build Graph.
    /// </summary>
    private void CoerceTargetPackageToCurrentGraph()
    {
        if (_graphService.Nodes.Count == 0)
        {
            return;
        }

        var t = (TargetPackage ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(t))
        {
            return;
        }

        var node = _graphService.GetNode(t);
        var configuredRepos = GetConfiguredRepoNameSet();
        if (node is not null && configuredRepos.Contains(node.RepoName))
        {
            return;
        }

        TargetPackage = string.Empty;
        _targetPreferences.SaveLastTargetPackage(string.Empty);
    }

    /// <summary>Graph build completes on a pool thread; <see cref="ObservableCollection"/> updates must run on the UI thread.</summary>
    private static void PostToUi(Action action)
    {
        var d = Application.Current?.Dispatcher;
        if (d is null || d.CheckAccess())
        {
            action();
        }
        else
        {
            d.Invoke(action);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _graphService.GraphChanged -= OnGraphChanged;
        _reset.ResetRequested -= OnResetRequested;
        GC.SuppressFinalize(this);
    }

    private void OnResetRequested(object? sender, EventArgs e)
    {
        IsGraphBuilt = false;
        TargetPackageChoices.Clear();
        TargetPackage = string.Empty;
        ImpactedPackages.Clear();
        ImpactHint = ImpactAnalysisPresentation.InitialHint;
        BuildOrder.Reset();
        _targetPreferences.SaveLastTargetPackage(string.Empty);
    }

    [RelayCommand]
    private async Task AnalyzeImpactAsync()
    {
        await RunImpactAnalysisAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        await RunImpactAnalysisAsync().ConfigureAwait(true);
        CalculateBuildOrder();
    }

    [RelayCommand]
    private async Task SelectPackageAndAnalyzeAsync(string? packageInclude)
    {
        if (string.IsNullOrWhiteSpace(packageInclude))
        {
            return;
        }

        TargetPackage = packageInclude.Trim();
        await RunImpactAnalysisAsync().ConfigureAwait(true);
    }

    private async Task RunImpactAnalysisAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetPackage))
        {
            ImpactHint = "Enter a package name.";
            _status.SetError("Enter a package name to analyze impact.");
            return;
        }

        IsAnalyzingImpact = true;
        _status.SetBusy("Analyzing impact…");
        try
        {
            await Task.Yield();
            var summary = _impactInteraction.AnalyzeAndHighlight(TargetPackage.Trim());
            ImpactedPackages.ReplaceAll(summary.Results);
            ImpactHint = summary.Hint;
            _targetPreferences.SaveLastTargetPackage(TargetPackage.Trim());
            var n = summary.Results.Count;
            _status.SetReady(n == 0
                ? "Impact analysis complete (no dependents found)."
                : $"Impact analysis complete ({n} package(s)).");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ImpactHint = $"Analysis failed: {ex.Message}";
            _status.SetError($"Impact analysis failed: {ex.Message}");
            _log.AppendTimestampedLine(_clock, $"Impact analysis failed: {ex}");
        }
        finally
        {
            IsAnalyzingImpact = false;
        }
    }

    [RelayCommand]
    private void CalculateBuildOrder()
    {
        if (string.IsNullOrWhiteSpace(TargetPackage))
        {
            _status.SetError("Enter a package name to calculate build order.");
            return;
        }

        var ok = BuildOrder.CalculateForTarget(TargetPackage);
        if (ok)
        {
            _status.SetReady(BuildOrder.Lines.Count == 0
                ? "Build order ready (no downstream steps)."
                : $"Build order: {BuildOrder.Lines.Count} step(s).");
        }
        else
        {
            _status.SetReady(BuildOrder.Hint);
        }
    }
}
