using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class WindowsCredentialTokenRewrapper : IStoredTokenRewrapper
{
    private readonly WindowsCredentialStore _store;

    public WindowsCredentialTokenRewrapper(WindowsCredentialStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public void RewrapTokens(IReadOnlyCollection<string> repoNames, byte[] oldKey, byte[] newKey)
    {
        foreach (var name in repoNames)
        {
            var raw = _store.GetPassword(name);
            if (raw is null)
            {
                continue;
            }

            string plain;
            if (TokenPayloadProtector.LooksLikeProtectedPayload(raw))
            {
                plain = TokenPayloadProtector.TryUnprotect(raw, oldKey) ?? raw;
            }
            else
            {
                plain = raw;
            }

            var rewrapped = TokenPayloadProtector.Protect(plain, newKey);
            _store.SavePassword(name, rewrapped);
        }
    }
}
