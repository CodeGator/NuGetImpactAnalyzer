using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <summary>
/// Shared log line formatting for view models (timestamp prefix).
/// </summary>
public static class ApplicationLogExtensions
{
    public static void AppendTimestampedLine(this IApplicationLog log, IClock clock, string message)
    {
        var stamp = clock.NowLocal.ToString("HH:mm:ss");
        log.AppendLine($"[{stamp}] {message}");
    }
}
