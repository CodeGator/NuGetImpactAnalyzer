using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NuGetImpactAnalyzer.Core;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Build-order output for the Impact tab; uses <see cref="IBuildOrderService"/> with a target supplied by the parent VM.
/// </summary>
public partial class ImpactBuildOrderViewModel : ObservableObject
{
    private readonly IBuildOrderService _buildOrder;

    public ObservableCollection<string> Lines { get; } = new();

    [ObservableProperty]
    private string _hint = ImpactAnalysisPresentation.BuildOrderInitialHint;

    public ImpactBuildOrderViewModel(IBuildOrderService buildOrder)
    {
        _buildOrder = buildOrder;
    }

    /// <summary>
    /// Computes build order for the given target (typically the parent tab's target package field).
    /// </summary>
    /// <returns><see langword="true"/> if a linear order was produced; <see langword="false"/> if empty graph, unknown target, cycle, etc.</returns>
    public bool CalculateForTarget(string? targetPackage)
    {
        Lines.Clear();
        var result = _buildOrder.GetBuildOrder(targetPackage ?? string.Empty);
        if (!result.Success)
        {
            Hint = result.ErrorMessage ?? "Could not compute build order.";
            return false;
        }

        Hint = ImpactAnalysisPresentation.FormatBuildOrderSuccessHint(result.OrderedPackages.Count);
        foreach (var line in ImpactAnalysisPresentation.FormatNumberedBuildOrderLines(result.OrderedPackages))
        {
            Lines.Add(line);
        }

        return true;
    }

    public void Reset()
    {
        Lines.Clear();
        Hint = ImpactAnalysisPresentation.BuildOrderInitialHint;
    }
}
