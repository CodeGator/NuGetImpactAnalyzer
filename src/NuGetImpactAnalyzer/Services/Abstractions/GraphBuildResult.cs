namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>
/// Outcome of building and formatting the dependency graph (application layer; not UI-specific).
/// </summary>
public sealed record GraphBuildResult(bool Success, string GraphText, string LogDetailLine);
