using System.Text.Json.Serialization;

namespace NuGetImpactAnalyzer.Models;

public sealed class AppConfig
{
    [JsonPropertyName("repos")]
    public List<Repo> Repos { get; set; } = [];
}
