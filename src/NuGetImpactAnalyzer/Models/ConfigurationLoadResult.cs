namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// Outcome of reading application configuration from storage.
/// </summary>
public sealed record ConfigurationLoadResult(
    AppConfig Config,
    string? Warning);
