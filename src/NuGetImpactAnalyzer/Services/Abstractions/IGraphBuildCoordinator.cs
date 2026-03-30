namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Builds the in-memory graph and produces display text plus a line for the application log.
/// </summary>
public interface IGraphBuildCoordinator
{
    Task<GraphBuildResult> BuildAsync(CancellationToken cancellationToken = default);
}
