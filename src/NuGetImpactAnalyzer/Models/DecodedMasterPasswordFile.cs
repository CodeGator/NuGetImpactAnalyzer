namespace NuGetImpactAnalyzer.Models;

/// <summary>Binary material decoded from <see cref="MasterPasswordFileData"/>.</summary>
public readonly struct DecodedMasterPasswordFile
{
    public DecodedMasterPasswordFile(byte[] passwordSalt, byte[] storedVerificationHash, byte[] keySalt)
    {
        PasswordSalt = passwordSalt;
        StoredVerificationHash = storedVerificationHash;
        KeySalt = keySalt;
    }

    public byte[] PasswordSalt { get; }

    public byte[] StoredVerificationHash { get; }

    public byte[] KeySalt { get; }
}
