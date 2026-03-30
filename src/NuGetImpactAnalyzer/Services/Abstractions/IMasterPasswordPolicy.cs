namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>Rules for acceptable master passwords (length, confirmation match).</summary>
public interface IMasterPasswordPolicy
{
    int MinimumLength { get; }

    bool TryValidateNewPassword(string password, string confirm, out string? error);
}
