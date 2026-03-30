namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Omits test-style projects from dependency graph construction so product projects are easier to read.
/// </summary>
/// <remarks>
/// Uses a case-insensitive substring match for <c>test</c> (so <c>UnitTests</c>, <c>MyApp.Tests</c>, etc. match).
/// Rare names like <c>Contoso</c> do not contain that substring; names such as <c>Contest</c> would be excluded.
/// </remarks>
public static class DependencyGraphProjectFilter
{
    /// <summary>
    /// True when <paramref name="projectName"/> should not appear as a graph node.
    /// </summary>
    public static bool ShouldExclude(string? projectName)
    {
        return !string.IsNullOrEmpty(projectName)
               && projectName.Contains("test", StringComparison.OrdinalIgnoreCase);
    }
}
