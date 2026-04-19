using System.Security.Cryptography;
using NuGetImpactAnalyzer.Models;
using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class MasterPasswordFileStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "NuGetImpactAnalyzerTests-" + Guid.NewGuid());

    public MasterPasswordFileStoreTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public void FileExists_WhenMissing_ReturnsFalse()
    {
        var sut = new MasterPasswordFileStore(_dir);

        Assert.False(sut.FileExists);
    }

    [Fact]
    public void Write_ThenTryRead_RoundTripsPayload()
    {
        var sut = new MasterPasswordFileStore(_dir);
        var data = new MasterPasswordFileData
        {
            PasswordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            PasswordHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeySalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
        };

        sut.Write(data);

        Assert.True(sut.FileExists);
        var read = sut.TryRead();
        Assert.True(read.Success, read.ErrorMessage);
        Assert.NotNull(read.Data);
        Assert.Equal(data.PasswordSalt, read.Data.PasswordSalt);
        Assert.Equal(data.PasswordHash, read.Data.PasswordHash);
        Assert.Equal(data.KeySalt, read.Data.KeySalt);
    }

    [Fact]
    public void TryRead_WhenFileMissing_ReturnsFailure()
    {
        var sut = new MasterPasswordFileStore(_dir);

        var read = sut.TryRead();

        Assert.False(read.Success);
        Assert.NotNull(read.ErrorMessage);
    }

    [Fact]
    public void TryRead_WhenJsonInvalid_ReturnsFailure()
    {
        var sut = new MasterPasswordFileStore(_dir);
        var path = Path.Combine(_dir, "master-password.json");
        File.WriteAllText(path, "{ not json");

        var read = sut.TryRead();

        Assert.False(read.Success);
        Assert.Contains("corrupted", read.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryDeleteFile_WhenMissing_ReturnsTrue()
    {
        var sut = new MasterPasswordFileStore(_dir);

        Assert.True(sut.TryDeleteFile(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryDeleteFile_WhenPresent_RemovesFile()
    {
        var sut = new MasterPasswordFileStore(_dir);
        var data = new MasterPasswordFileData
        {
            PasswordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
            PasswordHash = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            KeySalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)),
        };
        sut.Write(data);

        Assert.True(sut.TryDeleteFile(out var error), error);
        Assert.Null(error);
        Assert.False(sut.FileExists);
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
