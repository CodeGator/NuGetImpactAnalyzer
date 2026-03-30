using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

public interface IParsingService
{
    /// <summary>
    /// Recursively discovers *.csproj under the local clone for <paramref name="repo"/> and parses references.
    /// </summary>
    Task<IReadOnlyList<ProjectInfo>> AnalyzeRepositoryAsync(Repo repo, CancellationToken cancellationToken = default);
}
