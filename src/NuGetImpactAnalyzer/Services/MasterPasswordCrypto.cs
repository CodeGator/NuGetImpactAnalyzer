using System.Security.Cryptography;
using System.Text;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class MasterPasswordCrypto : IMasterPasswordCrypto
{
    public const int Pbkdf2Iterations = 310_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <inheritdoc />
    public NewMasterPasswordSecrets CreateSecretsForNewPassword(string password)
    {
        var passwordSalt = RandomNumberGenerator.GetBytes(SaltSize);
        var keySalt = RandomNumberGenerator.GetBytes(SaltSize);
        var verificationHash = Pbkdf2Sha256(password, passwordSalt, HashSize);
        var file = new MasterPasswordFileData
        {
            PasswordSalt = Convert.ToBase64String(passwordSalt),
            PasswordHash = Convert.ToBase64String(verificationHash),
            KeySalt = Convert.ToBase64String(keySalt),
        };
        var tokenKey = Pbkdf2Sha256(password, keySalt, HashSize);
        return new NewMasterPasswordSecrets(file, tokenKey);
    }

    /// <inheritdoc />
    public byte[] DeriveTokenProtectionKey(string password, ReadOnlySpan<byte> keySalt)
    {
        return Pbkdf2Sha256(password, keySalt, HashSize);
    }

    /// <inheritdoc />
    public bool TryDecodeStoredFile(MasterPasswordFileData file, out DecodedMasterPasswordFile decoded, out string? error)
    {
        decoded = default;
        error = null;
        if (file.PasswordSalt.Length == 0
            || file.PasswordHash.Length == 0
            || file.KeySalt.Length == 0)
        {
            error = "Master password file is invalid.";
            return false;
        }

        try
        {
            var passwordSalt = Convert.FromBase64String(file.PasswordSalt);
            var storedHash = Convert.FromBase64String(file.PasswordHash);
            var keySalt = Convert.FromBase64String(file.KeySalt);
            decoded = new DecodedMasterPasswordFile(passwordSalt, storedHash, keySalt);
            return true;
        }
        catch (FormatException)
        {
            error = "Master password file is corrupted.";
            return false;
        }
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, DecodedMasterPasswordFile decoded)
    {
        var candidate = Pbkdf2Sha256(password, decoded.PasswordSalt, HashSize);
        var ok = CryptographicOperations.FixedTimeEquals(candidate, decoded.StoredVerificationHash);
        CryptographicOperations.ZeroMemory(candidate);
        return ok;
    }

    private static byte[] Pbkdf2Sha256(string password, ReadOnlySpan<byte> salt, int outputBytes)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            outputBytes);
    }
}
