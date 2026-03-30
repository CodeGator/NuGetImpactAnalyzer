namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Broadcasts a request to clear analysis state when configuration changes (e.g. repository list edits).
/// </summary>
public interface IAnalysisResetService
{
    event EventHandler? ResetRequested;

    void RequestReset();
}

