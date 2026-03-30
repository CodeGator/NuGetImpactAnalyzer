using System.Security.Cryptography;
using System.Text;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class MasterPasswordCryptoTests
{
    [Fact]
    public void DeriveTokenProtectionKey_MatchesRfc2898WithSameParameters()
    {
        var sut = new MasterPasswordCrypto();
        var salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
        var expected = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes("unit-test-password"),
            salt,
            MasterPasswordCrypto.Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            32);

        var actual = sut.DeriveTokenProtectionKey("unit-test-password", salt);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void VerifyPassword_AcceptsPasswordUsedToCreateSecrets()
    {
        var sut = new MasterPasswordCrypto();
        var secrets = sut.CreateSecretsForNewPassword("correct-horse-long");

        Assert.True(sut.TryDecodeStoredFile(secrets.FilePayload, out var decoded, out var decodeErr), decodeErr);
        Assert.True(sut.VerifyPassword("correct-horse-long", decoded));
    }

    [Fact]
    public void VerifyPassword_RejectsWrongPassword()
    {
        var sut = new MasterPasswordCrypto();
        var secrets = sut.CreateSecretsForNewPassword("correct-horse-long");

        Assert.True(sut.TryDecodeStoredFile(secrets.FilePayload, out var decoded, out _));
        Assert.False(sut.VerifyPassword("wrong-password-x", decoded));
    }

    [Fact]
    public void TryDecodeStoredFile_InvalidBase64_ReturnsFalse()
    {
        var sut = new MasterPasswordCrypto();
        var bad = new MasterPasswordFileData
        {
            PasswordSalt = "not-base64!!!",
            PasswordHash = Convert.ToBase64String(new byte[32]),
            KeySalt = Convert.ToBase64String(new byte[16]),
        };

        Assert.False(sut.TryDecodeStoredFile(bad, out _, out var err));
        Assert.Equal("Master password file is corrupted.", err);
    }

    [Fact]
    public void TryDecodeStoredFile_EmptyFields_ReturnsFalse()
    {
        var sut = new MasterPasswordCrypto();
        var empty = new MasterPasswordFileData();

        Assert.False(sut.TryDecodeStoredFile(empty, out _, out var err));
        Assert.Equal("Master password file is invalid.", err);
    }
}
