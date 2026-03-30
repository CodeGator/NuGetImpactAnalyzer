using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Loads and saves per-user UI preferences (e.g. last impact target package).
/// </summary>
public interface IUserPreferencesService
{
    UserPreferences Load();

    void Save(UserPreferences preferences);
}
