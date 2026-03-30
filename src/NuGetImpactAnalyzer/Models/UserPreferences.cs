using System.Text.Json.Serialization;

namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// Persisted UI preferences (separate from <see cref="AppConfig"/> repository list).
/// </summary>
public sealed class UserPreferences
{
    [JsonPropertyName("lastTargetPackage")]
    public string LastTargetPackage { get; set; } = string.Empty;
}
