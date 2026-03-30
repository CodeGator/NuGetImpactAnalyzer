namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Append-only application output (e.g. status panel). Implementations are responsible for thread-safe UI updates when used from WPF.
/// </summary>
public interface IApplicationLog
{
    void AppendLine(string message);
}
