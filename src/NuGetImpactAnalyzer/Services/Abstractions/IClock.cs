namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Abstraction for current time (view models and logging use local timestamps).
/// </summary>
public interface IClock
{
    DateTime NowLocal { get; }
}
