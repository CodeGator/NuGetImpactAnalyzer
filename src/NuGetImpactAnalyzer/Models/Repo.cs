using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NuGetImpactAnalyzer.Models;

/// <summary>
/// A configured Git remote and branch listed in config.json.
/// </summary>
public partial class Repo : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _branch = "main";

    /// <summary>
    /// Path to the root .csproj for dependency analysis, relative to the repository root (forward slashes ok).
    /// When empty, all projects in the clone are included (legacy behavior).
    /// </summary>
    [ObservableProperty]
    private string _analysisProjectRelativePath = string.Empty;

    /// <summary>
    /// When the remote is GitHub, set from the REST API (<c>repository.private</c>). Not persisted; null if unknown or not GitHub.
    /// </summary>
    [ObservableProperty]
    [property: JsonIgnore]
    private bool? _gitHubIsPrivate;

    /// <summary>
    /// Whether Windows Credential Manager has a stored HTTPS token for <see cref="Name"/>. Not persisted.
    /// </summary>
    [ObservableProperty]
    [property: JsonIgnore]
    private bool _hasStoredCredentials;
}
