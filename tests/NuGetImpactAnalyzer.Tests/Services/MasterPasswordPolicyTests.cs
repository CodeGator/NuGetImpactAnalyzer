using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class MasterPasswordPolicyTests
{
    private readonly MasterPasswordPolicy _sut = new();

    [Fact]
    public void TryValidateNewPassword_TooShort_ReturnsFalse()
    {
        Assert.False(_sut.TryValidateNewPassword("short", "short", out var err));
        Assert.Contains("at least", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidateNewPassword_Mismatch_ReturnsFalse()
    {
        Assert.False(_sut.TryValidateNewPassword("longenougha", "longenoughb", out var err));
        Assert.Equal("Passwords do not match.", err);
    }

    [Fact]
    public void TryValidateNewPassword_Valid_ReturnsTrue()
    {
        Assert.True(_sut.TryValidateNewPassword("longenoughpw", "longenoughpw", out var err));
        Assert.Null(err);
    }

    [Fact]
    public void MinimumLength_MatchesPublicConstant()
    {
        Assert.Equal(MasterPasswordPolicy.DefaultMinimumLength, _sut.MinimumLength);
    }
}
