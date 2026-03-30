using System.Reflection;

namespace NuGetImpactAnalyzer.Core;

/// <summary>Builds the standard About dialog text (title, version, copyright).</summary>
public static class ApplicationAbout
{
    /// <summary>
    /// The first year used in the copyright range.
    /// </summary>
    public const int CopyrightStartYear = 2002;

    /// <summary>Multi-line message: title, blank line, version line, blank line, copyright with “All rights reserved”.</summary>
    public static string FormatAboutMessage()
    {
        var title = AppConstants.ApplicationTitle;
        var version = GetDisplayVersion();
        var copyright = FormatCopyrightLine();
        return $"{title}\n\nVersion {version}\n\n{copyright}";
    }

    /// <summary>
    /// Single-line copyright string for About UI.
    /// </summary>
    public static string FormatCopyrightLine() =>
        $"Copyright {CopyrightStartYear} - {DateTime.Now.Year} by CodeGator. All rights reserved.";

    /// <summary>
    /// Application version for display (informational version without build metadata when present).
    /// </summary>
    public static string GetProductVersion() => GetDisplayVersion();

    private static string GetDisplayVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return asm.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
