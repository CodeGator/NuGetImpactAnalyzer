using NuGetImpactAnalyzer.Services;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class SystemClockTests
{
    [Fact]
    public void NowLocal_IsLocalKindAndNearUtcNow()
    {
        var sut = new SystemClock();
        var before = DateTime.UtcNow;

        var t = sut.NowLocal;

        var after = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Local, t.Kind);
        var utc = t.ToUniversalTime();
        Assert.InRange(utc, before.AddSeconds(-2), after.AddSeconds(2));
    }
}
