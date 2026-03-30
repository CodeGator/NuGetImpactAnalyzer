using System.Security.Cryptography;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class MasterPasswordService : IMasterPasswordService, IDisposable
{
    private readonly IMasterPasswordFileStore _fileStore;
    private readonly IMasterPasswordCrypto _crypto;
    private readonly IMasterPasswordPolicy _policy;
    private readonly IStoredTokenRewrapper _tokenRewrapper;
    private readonly TokenStorageIndex _tokenIndex;
    private byte[]? _dataKey;
    private bool _disposed;

    public MasterPasswordService(
        IMasterPasswordFileStore masterPasswordFileStore,
        IMasterPasswordCrypto masterPasswordCrypto,
        IMasterPasswordPolicy masterPasswordPolicy,
        IStoredTokenRewrapper storedTokenRewrapper,
        TokenStorageIndex tokenStorageIndex)
    {
        _fileStore = masterPasswordFileStore;
        _crypto = masterPasswordCrypto;
        _policy = masterPasswordPolicy;
        _tokenRewrapper = storedTokenRewrapper;
        _tokenIndex = tokenStorageIndex;
    }

    /// <summary>Default directory for app vault files (master password payload, token index).</summary>
    public static string DefaultStorageDirectory() => MasterPasswordFileStore.DefaultStorageDirectory();

    /// <inheritdoc />
    public bool HasMasterPassword => _fileStore.FileExists;

    /// <inheritdoc />
    public bool IsUnlocked => _dataKey is not null;

    /// <inheritdoc />
    public byte[]? GetTokenProtectionKey() => _dataKey;

    /// <inheritdoc />
    public bool TryCreateMasterPassword(string password, string confirm, out string? error)
    {
        error = null;
        if (HasMasterPassword)
        {
            error = "A master password is already configured.";
            return false;
        }

        if (!_policy.TryValidateNewPassword(password, confirm, out error))
        {
            return false;
        }

        var secrets = _crypto.CreateSecretsForNewPassword(password);
        _fileStore.Write(secrets.FilePayload);
        _dataKey = secrets.TokenProtectionKey;
        return true;
    }

    /// <inheritdoc />
    public bool TryUnlock(string password, out string? error)
    {
        error = null;
        if (!HasMasterPassword)
        {
            error = "No master password is configured yet.";
            return false;
        }

        var read = _fileStore.TryRead();
        if (!read.Success || read.Data is null)
        {
            error = read.ErrorMessage ?? "Could not read master password file.";
            return false;
        }

        if (!_crypto.TryDecodeStoredFile(read.Data, out var decoded, out var decodeError))
        {
            error = decodeError;
            return false;
        }

        if (!_crypto.VerifyPassword(password, decoded))
        {
            error = "Incorrect master password.";
            return false;
        }

        _dataKey = _crypto.DeriveTokenProtectionKey(password, decoded.KeySalt);
        return true;
    }

    /// <inheritdoc />
    public void Lock()
    {
        if (_dataKey is not null)
        {
            CryptographicOperations.ZeroMemory(_dataKey);
            _dataKey = null;
        }
    }

    /// <inheritdoc />
    public bool TryChangeMasterPassword(
        string currentPassword,
        string newPassword,
        string confirm,
        IReadOnlyList<string> additionalRepoNames,
        out string? error)
    {
        error = null;
        if (!IsUnlocked || _dataKey is null)
        {
            error = "Session is locked.";
            return false;
        }

        if (!_policy.TryValidateNewPassword(newPassword, confirm, out error))
        {
            return false;
        }

        var read = _fileStore.TryRead();
        if (!read.Success || read.Data is null)
        {
            error = read.ErrorMessage ?? "Could not read master password file.";
            return false;
        }

        if (!_crypto.TryDecodeStoredFile(read.Data, out var decoded, out var decodeError))
        {
            error = decodeError;
            return false;
        }

        if (!_crypto.VerifyPassword(currentPassword, decoded))
        {
            error = "Current master password is incorrect.";
            return false;
        }

        var oldKey = _dataKey;
        var newSecrets = _crypto.CreateSecretsForNewPassword(newPassword);

        var names = new HashSet<string>(_tokenIndex.GetAll(), StringComparer.OrdinalIgnoreCase);
        foreach (var n in additionalRepoNames)
        {
            if (!string.IsNullOrWhiteSpace(n))
            {
                names.Add(n.Trim());
            }
        }

        try
        {
            _tokenRewrapper.RewrapTokens(names, oldKey, newSecrets.TokenProtectionKey);
        }
        catch (Exception ex)
        {
            CryptographicOperations.ZeroMemory(newSecrets.TokenProtectionKey);
            error = $"Could not re-protect stored tokens: {ex.Message}";
            return false;
        }

        _fileStore.Write(newSecrets.FilePayload);
        CryptographicOperations.ZeroMemory(oldKey);
        _dataKey = newSecrets.TokenProtectionKey;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Lock();
        _disposed = true;
    }
}
