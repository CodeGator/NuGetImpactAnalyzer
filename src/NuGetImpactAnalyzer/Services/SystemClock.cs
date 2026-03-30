using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class SystemClock : IClock
{
    public DateTime NowLocal => DateTime.Now;
}
