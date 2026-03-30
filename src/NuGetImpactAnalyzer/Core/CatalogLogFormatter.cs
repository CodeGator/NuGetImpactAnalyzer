using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Core;

internal static class CatalogLogFormatter
{
    public static IEnumerable<string> Format(ConfigurationLoadResult result)
    {
        var stamp = Timestamp();
        yield return $"[{stamp}] Loaded {result.Config.Repos.Count} repo(s) from config.";
        if (result.Warning is not null)
        {
            yield return $"[{stamp}] {result.Warning}";
        }
    }

    private static string Timestamp() => DateTime.Now.ToString("HH:mm:ss");
}
