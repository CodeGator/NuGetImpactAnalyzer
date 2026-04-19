using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class VaultSecretsResetServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "NuGetImpactAnalyzerTests-" + Guid.NewGuid());
    private readonly string _repoName = "vault-reset-test-" + Guid.NewGuid();
    private readonly TokenStorageIndex _index;
    private readonly MasterPasswordFileStore _fileStore;
    private readonly WindowsCredentialStore _credStore;
    private readonly MasterPasswordService _master;

    public VaultSecretsResetServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _index = new TokenStorageIndex(_dir);
        _fileStore = new MasterPasswordFileStore(_dir);
        _credStore = new WindowsCredentialStore();
        _master = new MasterPasswordService(
            _fileStore,
            new MasterPasswordCrypto(),
            new MasterPasswordPolicy(),
            new WindowsCredentialTokenRewrapper(_credStore),
            _index);
        Assert.True(_master.TryCreateMasterPassword("longenoughpw", "longenoughpw", out var err), err);
    }

    [Fact]
    public void TryResetAfterForgottenMasterPassword_RemovesMasterFileTokenIndexAndStoredPat()
    {
        Assert.True(_master.TryUnlock("longenoughpw", out var unlockErr), unlockErr);
        var credService = new ProtectedCredentialService(_credStore, _master, _index);
        credService.SaveToken(_repoName, "secret-pat-value");
        _master.Lock();

        var sut = new VaultSecretsResetService(_master, _fileStore, _index, _credStore);

        Assert.True(sut.TryResetAfterForgottenMasterPassword(out var resetErr), resetErr);

        Assert.False(_fileStore.FileExists);
        Assert.Empty(_index.GetAll());
        Assert.True(_master.TryCreateMasterPassword("newlongpw", "newlongpw", out var createErr), createErr);
        Assert.True(_master.TryUnlock("newlongpw", out var unlock2), unlock2);
        var cred2 = new ProtectedCredentialService(_credStore, _master, _index);
        Assert.Null(cred2.GetToken(_repoName));
    }

    public void Dispose()
    {
        try
        {
            foreach (var name in _index.GetAll())
            {
                _credStore.Delete(name);
            }

            _credStore.Delete(_repoName);
        }
        catch
        {
        }

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
}
