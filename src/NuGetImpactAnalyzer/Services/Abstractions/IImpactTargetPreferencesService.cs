namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Persists the last impact-analysis target package id without exposing full <see cref="Models.UserPreferences"/> to view models.
/// </summary>
public interface IImpactTargetPreferencesService
{
    /// <returns>Trimmed package id, or null if none stored.</returns>
    string? LoadLastTargetPackage();

    void SaveLastTargetPackage(string packageId);
}
