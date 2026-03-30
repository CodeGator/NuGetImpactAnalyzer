using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

public sealed class AnalysisResetService : IAnalysisResetService
{
    public event EventHandler? ResetRequested;

    public void RequestReset() => ResetRequested?.Invoke(this, EventArgs.Empty);
}

