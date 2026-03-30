namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Stores per-repository tokens in Windows Credential Manager (current user).
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Persists a personal access token (or password) for HTTPS Git access to the named repository.
    /// </summary>
    void SaveToken(string repoName, string token);

    /// <summary>
    /// Returns the stored token, or null if none.
    /// </summary>
    string? GetToken(string repoName);

    /// <summary>
    /// Removes stored credentials for the repository.
    /// </summary>
    void DeleteToken(string repoName);
}
