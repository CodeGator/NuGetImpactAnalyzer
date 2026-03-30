using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class MasterPasswordServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "NuGetImpactAnalyzerTests-" + Guid.NewGuid());
    private readonly TokenStorageIndex _index;
    private readonly MasterPasswordFileStore _fileStore;
    private readonly MasterPasswordCrypto _crypto;
    private readonly MasterPasswordPolicy _policy;
    private readonly RecordingTokenRewrapper _rewrapper;

    public MasterPasswordServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _index = new TokenStorageIndex(_dir);
        _fileStore = new MasterPasswordFileStore(_dir);
        _crypto = new MasterPasswordCrypto();
        _policy = new MasterPasswordPolicy();
        _rewrapper = new RecordingTokenRewrapper();
    }

    private MasterPasswordService CreateSut() =>
        new(_fileStore, _crypto, _policy, _rewrapper, _index);

    [Fact]
    public void TryCreateMasterPassword_WhenFileAlreadyExists_ReturnsFalse()
    {
        var sut = CreateSut();
        Assert.True(sut.TryCreateMasterPassword("first-password1", "first-password1", out _));

        Assert.False(sut.TryCreateMasterPassword("second-password2", "second-password2", out var err));
        Assert.NotNull(err);
    }

    [Fact]
    public void TryUnlock_WrongPassword_ReturnsFalse()
    {
        var sut = CreateSut();
        Assert.True(sut.TryCreateMasterPassword("right-password1", "right-password1", out _));
        sut.Lock();

        Assert.False(sut.TryUnlock("wrong-password1", out var err));
        Assert.Equal("Incorrect master password.", err);
    }

    [Fact]
    public void TryUnlock_CorrectPassword_ReturnsTrue()
    {
        var sut = CreateSut();
        Assert.True(sut.TryCreateMasterPassword("right-password1", "right-password1", out _));
        sut.Lock();

        Assert.True(sut.TryUnlock("right-password1", out var err), err);
    }

    [Fact]
    public void TryChangeMasterPassword_RewrapsUnionOfIndexAndAdditionalRepos()
    {
        var sut = CreateSut();
        Assert.True(sut.TryCreateMasterPassword("old-password-1", "old-password-1", out _));
        _index.Add("from-index");
        Assert.True(sut.TryChangeMasterPassword(
            "old-password-1",
            "new-password-2",
            "new-password-2",
            ["  extra-repo  "],
            out var err), err);

        Assert.Single(_rewrapper.Calls);
        var names = _rewrapper.Calls[0].RepoNames;
        Assert.Contains("from-index", names);
        Assert.Contains("extra-repo", names);

        sut.Lock();
        Assert.False(sut.TryUnlock("old-password-1", out _));
        Assert.True(sut.TryUnlock("new-password-2", out var errUnlock), errUnlock);
    }

    [Fact]
    public void TryChangeMasterPassword_WhenRewrapThrows_DoesNotReplaceStoredFile()
    {
        _rewrapper.NextThrow = new InvalidOperationException("credential store unavailable");
        var sut = CreateSut();
        Assert.True(sut.TryCreateMasterPassword("old-password-1", "old-password-1", out _));

        Assert.False(sut.TryChangeMasterPassword(
            "old-password-1",
            "new-password-2",
            "new-password-2",
            [],
            out var err));
        Assert.Contains("credential store unavailable", err);

        sut.Lock();
        Assert.True(sut.TryUnlock("old-password-1", out var unlockErr), unlockErr);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }
        catch
        {
        }
    }

    private sealed class RecordingTokenRewrapper : IStoredTokenRewrapper
    {
        public List<(IReadOnlyCollection<string> RepoNames, byte[] OldKey, byte[] NewKey)> Calls { get; } = [];

        public Exception? NextThrow { get; set; }

        public void RewrapTokens(IReadOnlyCollection<string> repoNames, byte[] oldKey, byte[] newKey)
        {
            if (NextThrow is not null)
            {
                var ex = NextThrow;
                NextThrow = null;
                throw ex;
            }

            Calls.Add((repoNames, oldKey.ToArray(), newKey.ToArray()));
        }
    }
}
