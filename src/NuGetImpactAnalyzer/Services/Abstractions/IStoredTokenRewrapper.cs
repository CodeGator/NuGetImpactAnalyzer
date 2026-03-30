namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>Re-encrypts stored HTTPS tokens when the master password (data key) changes.</summary>
public interface IStoredTokenRewrapper
{
    void RewrapTokens(IReadOnlyCollection<string> repoNames, byte[] oldKey, byte[] newKey);
}
