using CommunityToolkit.Mvvm.ComponentModel;
using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// One vertex in the graph canvas (binding target for a node box).
/// </summary>
public partial class GraphVisualNodeViewModel : ObservableObject
{
    public string NodeId { get; }

    public string Label { get; }

    public double X { get; }

    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private ImpactSeverity _impactSeverity = ImpactSeverity.None;

    public GraphVisualNodeViewModel(
        string nodeId,
        string label,
        double x,
        double y,
        double width,
        double height)
    {
        NodeId = nodeId;
        Label = label;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public double CenterX => X + Width / 2;

    public double CenterY => Y + Height / 2;
}
