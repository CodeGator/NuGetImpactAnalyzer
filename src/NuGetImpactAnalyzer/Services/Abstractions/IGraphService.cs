using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Services.Abstractions;

public interface IGraphService
{
    /// <summary>
    /// Raised after <see cref="BuildGraphAsync"/> successfully replaces the in-memory graph.
    /// </summary>
    event EventHandler? GraphChanged;

    /// <summary>
    /// Clears and rebuilds the in-memory graph from configured repositories (parses each clone).
    /// </summary>
    Task BuildGraphAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the in-memory graph and raises <see cref="GraphChanged"/>.
    /// </summary>
    void ClearGraph();

    /// <summary>
    /// Looks up a node by full id (<c>repo/project</c>), or by unique short <see cref="GraphNode.Name"/>.
    /// </summary>
    GraphNode? GetNode(string name);

    IReadOnlyDictionary<string, GraphNode> Nodes { get; }

    /// <summary>
    /// Simple text rendering: <c>A -&gt; B, C</c> lines for the current graph.
    /// </summary>
    string FormatGraphText();
}
