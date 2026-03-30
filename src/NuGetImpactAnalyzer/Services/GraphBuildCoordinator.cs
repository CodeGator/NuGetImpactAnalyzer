using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class GraphBuildCoordinator : IGraphBuildCoordinator
{
    private readonly IGraphService _graphService;

    public GraphBuildCoordinator(IGraphService graphService)
    {
        _graphService = graphService;
    }

    /// <inheritdoc />
    public async Task<GraphBuildResult> BuildAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _graphService.BuildGraphAsync(cancellationToken).ConfigureAwait(false);
            var text = _graphService.FormatGraphText();
            var count = _graphService.Nodes.Count;
            return new GraphBuildResult(true, text, $"Graph built: {count} node(s).");
        }
        catch (Exception ex)
        {
            return new GraphBuildResult(false, $"Could not build graph: {ex.Message}", $"Graph build failed: {ex.Message}");
        }
    }
}
