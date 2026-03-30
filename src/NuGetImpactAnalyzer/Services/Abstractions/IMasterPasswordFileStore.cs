using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>Persists the master-password verification file on disk.</summary>
public interface IMasterPasswordFileStore
{
    bool FileExists { get; }

    MasterPasswordReadResult TryRead();

    void Write(MasterPasswordFileData data);
}
