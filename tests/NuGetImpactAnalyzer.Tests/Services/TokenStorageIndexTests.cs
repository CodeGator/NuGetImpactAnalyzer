using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class TokenStorageIndexTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "NuGetImpactAnalyzerTests-" + Guid.NewGuid());

    public TokenStorageIndexTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public void ClearAll_RemovesAllNamesFromDisk()
    {
        var sut = new TokenStorageIndex(_dir);
        sut.Add("repo-a");
        sut.Add("repo-b");
        Assert.Equal(2, sut.GetAll().Count);

        sut.ClearAll();

        Assert.Empty(sut.GetAll());
        var reloaded = new TokenStorageIndex(_dir);
        Assert.Empty(reloaded.GetAll());
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
}
