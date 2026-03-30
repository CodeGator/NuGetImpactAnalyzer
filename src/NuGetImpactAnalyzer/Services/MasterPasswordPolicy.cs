using NuGetImpactAnalyzer.Services.Abstractions;

namespace NuGetImpactAnalyzer.Services;

/// <inheritdoc />
public sealed class MasterPasswordPolicy : IMasterPasswordPolicy
{
    /// <summary>Shared default for UI and validation messages.</summary>
    public const int DefaultMinimumLength = 8;

    /// <inheritdoc />
    public int MinimumLength => DefaultMinimumLength;

    /// <inheritdoc />
    public bool TryValidateNewPassword(string password, string confirm, out string? error)
    {
        error = null;
        if (password.Length < MinimumLength)
        {
            error = $"Password must be at least {MinimumLength} characters.";
            return false;
        }

        if (password != confirm)
        {
            error = "Passwords do not match.";
            return false;
        }

        return true;
    }
}
