namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>Clears PAT vault data and the master-password file after the user confirms they forgot the master password.</summary>
public interface IVaultSecretsResetService
{
    /// <summary>
    /// Deletes stored HTTPS tokens from Windows Credential Manager for every repository in the token index,
    /// clears the token index, removes the master-password file, and clears any in-memory protection key.
    /// Does not modify preferences, parse cache, configuration, or cloned repositories.
    /// </summary>
    /// <param name="error">Populated when the master-password file could not be deleted.</param>
    /// <returns>True when the master-password file was absent or successfully deleted.</returns>
    bool TryResetAfterForgottenMasterPassword(out string? error);
}
