namespace NuGetImpactAnalyzer.Services.Abstractions;

/// <summary>Outcome of checking whether a URL refers to an accessible GitHub repository.</summary>
public sealed record GitHubRepositoryUrlValidationResult(bool IsValid, string? ErrorMessage = null);
