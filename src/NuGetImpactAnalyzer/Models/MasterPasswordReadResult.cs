namespace NuGetImpactAnalyzer.Models;

/// <summary>Result of reading <c>master-password.json</c> from disk.</summary>
public sealed class MasterPasswordReadResult
{
    public required bool Success { get; init; }

    public MasterPasswordFileData? Data { get; init; }

    public string? ErrorMessage { get; init; }

    public static MasterPasswordReadResult Ok(MasterPasswordFileData data) =>
        new() { Success = true, Data = data };

    public static MasterPasswordReadResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
