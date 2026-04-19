using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class VaultSecretsResetService : IVaultSecretsResetService
{
    private readonly IMasterPasswordService _master;
    private readonly IMasterPasswordFileStore _fileStore;
    private readonly TokenStorageIndex _tokenIndex;
    private readonly WindowsCredentialStore _credentialStore;

    public VaultSecretsResetService(
        IMasterPasswordService masterPasswordService,
        IMasterPasswordFileStore masterPasswordFileStore,
        TokenStorageIndex tokenStorageIndex,
        WindowsCredentialStore credentialStore)
    {
        _master = masterPasswordService;
        _fileStore = masterPasswordFileStore;
        _tokenIndex = tokenStorageIndex;
        _credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public bool TryResetAfterForgottenMasterPassword(out string? error)
    {
        error = null;
        _master.Lock();

        foreach (var repoName in _tokenIndex.GetAll())
        {
            _credentialStore.Delete(repoName);
        }

        _tokenIndex.ClearAll();

        if (!_fileStore.TryDeleteFile(out error))
        {
            return false;
        }

        return true;
    }
}
