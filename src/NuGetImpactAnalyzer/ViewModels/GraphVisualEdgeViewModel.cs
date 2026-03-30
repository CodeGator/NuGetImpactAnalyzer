namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// Directed edge segment for a line on the graph canvas.
/// </summary>
public sealed class GraphVisualEdgeViewModel
{
    public double X1 { get; init; }

    public double Y1 { get; init; }

    public double X2 { get; init; }

    public double Y2 { get; init; }
}
