using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Loads strongly typed application configuration (e.g. repository list) from the configured source.
/// </summary>
public interface IAppConfigurationService
{
    /// <summary>
    /// Reads the current configuration. Missing or invalid data is represented by an empty
    /// <see cref="AppConfig"/> and a non-null <see cref="ConfigurationLoadResult.Warning"/>.
    /// </summary>
    ConfigurationLoadResult Load();

    /// <summary>
    /// Writes the full application configuration (including the repository list) to storage.
    /// </summary>
    void Save(AppConfig config);
}
