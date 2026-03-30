using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>PBKDF2 and related operations for the master password vault (no I/O).</summary>
public interface IMasterPasswordCrypto
{
    /// <summary>Generate salts and file fields; derive the token protection key (32 bytes).</summary>
    NewMasterPasswordSecrets CreateSecretsForNewPassword(string password);

    byte[] DeriveTokenProtectionKey(string password, ReadOnlySpan<byte> keySalt);

    bool TryDecodeStoredFile(MasterPasswordFileData file, out DecodedMasterPasswordFile decoded, out string? error);

    /// <summary>Derives the verification digest and compares with the stored verification hash (timing-safe).</summary>
    bool VerifyPassword(string password, DecodedMasterPasswordFile decoded);
}
