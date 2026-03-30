using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class CredentialServiceTests
{
    [Fact]
    public void BuildTarget_IncludesConstantPrefix()
    {
        var target = WindowsCredentialStore.BuildTarget("MyRepo");

        Assert.StartsWith(WindowsCredentialStore.TargetPrefix, target, StringComparison.Ordinal);
        Assert.Equal("NuGetImpactAnalyzer:repo:MyRepo", target);
    }

    [Fact]
    public void BuildTarget_TrimsRepositoryName()
    {
        var target = WindowsCredentialStore.BuildTarget("  spaced  ");

        Assert.Equal("NuGetImpactAnalyzer:repo:spaced", target);
    }

    [Fact]
    public void BuildTarget_ReplacesInvalidFileNameCharactersWithUnderscore()
    {
        var target = WindowsCredentialStore.BuildTarget("a<b>c|d");

        Assert.DoesNotContain("<", target);
        Assert.Equal("NuGetImpactAnalyzer:repo:a_b_c_d", target);
    }

    [Fact]
    public void BuildTarget_ReplacesColonAndAsteriskEvenWhenValidInPath()
    {
        var target = WindowsCredentialStore.BuildTarget("ns:repo*suffix");

        Assert.Equal("NuGetImpactAnalyzer:repo:ns_repo_suffix", target);
    }

    [Fact]
    public void SaveToken_WhenRepoNameNull_ThrowsArgumentException()
    {
        using var harness = UnlockedVaultHarness.Create();

        Assert.Throws<ArgumentException>(() => harness.Service.SaveToken(null!, "pat"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void SaveToken_WhenRepoNameEmptyOrWhitespace_ThrowsArgumentException(string repoName)
    {
        using var harness = UnlockedVaultHarness.Create();

        Assert.Throws<ArgumentException>(() => harness.Service.SaveToken(repoName, "pat"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetToken_WhenRepoNameWhitespace_ReturnsNull(string repoName)
    {
        using var harness = UnlockedVaultHarness.Create();

        Assert.Null(harness.Service.GetToken(repoName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeleteToken_WhenRepoNameWhitespace_DoesNotThrow(string repoName)
    {
        using var harness = UnlockedVaultHarness.Create();

        var ex = Record.Exception(() => harness.Service.DeleteToken(repoName));

        Assert.Null(ex);
    }

    [Fact]
    public void SaveToken_WhenVaultLocked_ThrowsInvalidOperationException()
    {
        var temp = Path.Combine(Path.GetTempPath(), "NuGetImpactAnalyzerTests-" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(temp);
            var store = new WindowsCredentialStore();
            var index = new TokenStorageIndex(temp);
            var master = CreateMasterPasswordService(temp, store, index);
            Assert.True(master.TryCreateMasterPassword("longenoughpw", "longenoughpw", out var err1), err1);
            master.Lock();
            var sut = new ProtectedCredentialService(store, master, index);

            Assert.Throws<InvalidOperationException>(() => sut.SaveToken("r", "token"));
        }
        finally
        {
            TryDeleteDirectory(temp);
        }
    }

    private sealed class UnlockedVaultHarness : IDisposable
    {
        private readonly string _temp;
        public ProtectedCredentialService Service { get; }

        private UnlockedVaultHarness(string temp, ProtectedCredentialService service)
        {
            _temp = temp;
            Service = service;
        }

        public static UnlockedVaultHarness Create()
        {
            var temp = Path.Combine(Path.GetTempPath(), "NuGetImpactAnalyzerTests-" + Guid.NewGuid());
            Directory.CreateDirectory(temp);
            var store = new WindowsCredentialStore();
            var index = new TokenStorageIndex(temp);
            var master = CreateMasterPasswordService(temp, store, index);
            Assert.True(master.TryCreateMasterPassword("longenoughpw", "longenoughpw", out var err), err);
            return new UnlockedVaultHarness(temp, new ProtectedCredentialService(store, master, index));
        }

        public void Dispose() => TryDeleteDirectory(_temp);
    }

    private static MasterPasswordService CreateMasterPasswordService(string temp, WindowsCredentialStore credStore, TokenStorageIndex index)
    {
        var fileStore = new MasterPasswordFileStore(temp);
        var crypto = new MasterPasswordCrypto();
        var policy = new MasterPasswordPolicy();
        var rewrapper = new WindowsCredentialTokenRewrapper(credStore);
        return new MasterPasswordService(fileStore, crypto, policy, rewrapper, index);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // test cleanup best-effort
        }
    }
}
