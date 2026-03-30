using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Encrypts PATs at rest in Windows Credential Manager using the session key from <see cref="IMasterPasswordService"/>.
/// </summary>
public sealed class ProtectedCredentialService : ICredentialService
{
    private readonly WindowsCredentialStore _store;
    private readonly IMasterPasswordService _master;
    private readonly TokenStorageIndex _tokenIndex;

    public ProtectedCredentialService(
        WindowsCredentialStore store,
        IMasterPasswordService master,
        TokenStorageIndex tokenIndex)
    {
        _store = store;
        _master = master;
        _tokenIndex = tokenIndex;
    }

    /// <inheritdoc />
    public void SaveToken(string repoName, string token)
    {
        if (!_master.IsUnlocked)
        {
            throw new InvalidOperationException("Cannot save credentials while the vault is locked.");
        }

        if (string.IsNullOrWhiteSpace(repoName))
        {
            throw new ArgumentException("Repository name is required.", nameof(repoName));
        }

        if (string.IsNullOrEmpty(token))
        {
            DeleteToken(repoName);
            return;
        }

        var key = _master.GetTokenProtectionKey();
        if (key is null)
        {
            throw new InvalidOperationException("Protection key is not available.");
        }

        try
        {
            var enc = TokenPayloadProtector.Protect(token, key);
            _store.SavePassword(repoName, enc);
            _tokenIndex.Add(repoName);
        }
        finally
        {
            // key is shared session buffer; do not zero here
        }
    }

    /// <inheritdoc />
    public string? GetToken(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return null;
        }

        if (!_master.IsUnlocked)
        {
            return null;
        }

        var key = _master.GetTokenProtectionKey();
        if (key is null)
        {
            return null;
        }

        var raw = _store.GetPassword(repoName);
        if (raw is null)
        {
            return null;
        }

        if (TokenPayloadProtector.LooksLikeProtectedPayload(raw))
        {
            return TokenPayloadProtector.TryUnprotect(raw, key);
        }

        // Legacy plaintext PAT saved before encryption was enabled
        return raw;
    }

    /// <inheritdoc />
    public void DeleteToken(string repoName)
    {
        if (string.IsNullOrWhiteSpace(repoName))
        {
            return;
        }

        _store.Delete(repoName);
        _tokenIndex.Remove(repoName);
    }
}
