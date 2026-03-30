using NuGetImpactAnalyzer.Services;
using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Tests.Services;

public sealed class ApplicationLogExtensionsTests
{
    private sealed class ListLog : IApplicationLog
    {
        public List<string> Lines { get; } = new();

        public void AppendLine(string message) => Lines.Add(message);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime NowLocal { get; init; }
    }

    [Fact]
    public void AppendTimestampedLine_PrefixesMessageWithBracketedTime()
    {
        var log = new ListLog();
        var clock = new FixedClock { NowLocal = new DateTime(2026, 3, 28, 9, 5, 7, DateTimeKind.Local) };

        log.AppendTimestampedLine(clock, "hello");

        Assert.Single(log.Lines);
        Assert.Equal("[09:05:07] hello", log.Lines[0]);
    }
}
