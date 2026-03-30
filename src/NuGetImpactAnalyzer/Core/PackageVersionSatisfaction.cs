using NuGet.Versioning;

namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Compares required NuGet version ranges to resolved versions using <see cref="NuGet.Versioning"/>.
/// </summary>
public static class PackageVersionSatisfaction
{
    /// <summary>
    /// Returns true when <paramref name="requiredConstraint"/> parses as a <see cref="VersionRange"/>
    /// that satisfies <paramref name="resolvedVersion"/> as a <see cref="NuGetVersion"/>.
    /// </summary>
    public static bool IsSatisfied(string? requiredConstraint, string? resolvedVersion)
    {
        // Treat a missing/unparseable constraint as "unconstrained" (definite edge).
        if (string.IsNullOrWhiteSpace(requiredConstraint))
        {
            return true;
        }

        if (!VersionRange.TryParse(requiredConstraint, out var range))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(resolvedVersion))
        {
            return false;
        }

        if (!NuGetVersion.TryParse(resolvedVersion, out var version))
        {
            return false;
        }

        return range.Satisfies(version);
    }
}
