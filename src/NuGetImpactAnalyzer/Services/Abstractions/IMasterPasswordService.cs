namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Master-password gate: verification file, session key for token protection, lock/unlock.
/// </summary>
public interface IMasterPasswordService
{
    /// <summary>True when a master password file exists on disk.</summary>
    bool HasMasterPassword { get; }

    /// <summary>True after successful create or unlock until <see cref="Lock"/>.</summary>
    bool IsUnlocked { get; }

    /// <summary>32-byte AES key when <see cref="IsUnlocked"/>; otherwise null.</summary>
    byte[]? GetTokenProtectionKey();

    bool TryCreateMasterPassword(string password, string confirm, out string? error);

    bool TryUnlock(string password, out string? error);

    void Lock();

    /// <summary>
    /// Re-wraps all indexed tokens plus <paramref name="additionalRepoNames"/> using a new master password.
    /// Must be called only while unlocked.
    /// </summary>
    bool TryChangeMasterPassword(
        string currentPassword,
        string newPassword,
        string confirm,
        IReadOnlyList<string> additionalRepoNames,
        out string? error);
}
