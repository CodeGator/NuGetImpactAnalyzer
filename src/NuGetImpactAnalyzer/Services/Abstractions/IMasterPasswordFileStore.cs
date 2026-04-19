using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>Persists the master-password verification file on disk.</summary>
public interface IMasterPasswordFileStore
{
    bool FileExists { get; }

    MasterPasswordReadResult TryRead();

    void Write(MasterPasswordFileData data);

    /// <summary>Deletes the persisted master-password file if it exists.</summary>
    /// <returns>False when the file exists but could not be deleted.</returns>
    bool TryDeleteFile(out string? error);
}
