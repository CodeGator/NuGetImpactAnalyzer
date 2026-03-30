using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services;

public interface IConfigService
{
    /// <summary>
    /// Loads application configuration from disk. Missing or invalid JSON yields an empty config;
    /// see <paramref name="warning"/> for details.
    /// </summary>
    AppConfig Load(out string? warning);
}
