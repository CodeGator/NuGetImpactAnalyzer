namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Application-wide status line and busy/error presentation (implemented by the shell status bar).
/// </summary>
public interface IApplicationStatus
{
    void SetReady(string message = "Ready");

    void SetBusy(string message);

    void SetError(string message);
}
