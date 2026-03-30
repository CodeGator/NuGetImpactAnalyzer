namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Supplies the repository name used as the credential key (e.g. draft name while editing a <c>Repo</c>).
/// </summary>
public interface IRepositoryCredentialContext
{
    /// <summary>
    /// Credential store key; typically the configured repository display name.
    /// </summary>
    string CredentialKeyName { get; }

    /// <summary>
    /// Raised when <see cref="CredentialKeyName"/> may have changed (e.g. user edited the name field).
    /// </summary>
    event EventHandler? CredentialKeyNameChanged;
}
