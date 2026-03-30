using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class ImpactTargetPreferencesService : IImpactTargetPreferencesService
{
    private readonly IUserPreferencesService _preferences;

    public ImpactTargetPreferencesService(IUserPreferencesService preferences)
    {
        _preferences = preferences;
    }

    /// <inheritdoc />
    public string? LoadLastTargetPackage()
    {
        var raw = _preferences.Load().LastTargetPackage;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim();
    }

    /// <inheritdoc />
    public void SaveLastTargetPackage(string packageId)
    {
        _preferences.Save(new UserPreferences { LastTargetPackage = packageId.Trim() });
    }
}
