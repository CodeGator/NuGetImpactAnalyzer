using NuGetImpactAnalyzer.Models;

namespace NuGetImpactAnalyzer.Core;

/// <summary>
/// Pure graph algorithms for dependency edges (dependent node id → dependency node ids).
/// </summary>
public static class DependencyGraphTraversal
{
    /// <summary>
    /// Finds node ids whose full id or short <see cref="GraphNode.Name"/> matches <paramref name="trimmedQuery"/> (case-insensitive).
    /// </summary>
    public static HashSet<string> ResolveMatchingNodeIds(
        IReadOnlyDictionary<string, GraphNode> nodes,
        string trimmedQuery)
    {
        var startIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in nodes)
        {
            if (kv.Key.Equals(trimmedQuery, StringComparison.OrdinalIgnoreCase) ||
                kv.Value.Name.Equals(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            {
                startIds.Add(kv.Key);
            }
        }

        return startIds;
    }

    /// <summary>
    /// Maps each dependency id → dependent ids that list it in <see cref="GraphNode.Dependencies"/>.
    /// </summary>
    public static Dictionary<string, List<string>> BuildReverseAdjacency(
        IReadOnlyDictionary<string, GraphNode> nodes)
    {
        var reverse = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in nodes)
        {
            var dependentId = kv.Key;
            foreach (var depId in kv.Value.Dependencies)
            {
                if (!reverse.TryGetValue(depId, out var list))
                {
                    list = new List<string>();
                    reverse[depId] = list;
                }

                list.Add(dependentId);
            }
        }

        return reverse;
    }

    /// <summary>
    /// All nodes that transitively depend on any of <paramref name="startNodeIds"/>, excluding the start nodes themselves.
    /// </summary>
    public static HashSet<string> CollectTransitiveDependentsOnly(
        HashSet<string> startNodeIds,
        Dictionary<string, List<string>> reverseAdjacency)
    {
        var impacted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        foreach (var id in startNodeIds)
        {
            queue.Enqueue(id);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!reverseAdjacency.TryGetValue(current, out var dependents))
            {
                continue;
            }

            foreach (var d in dependents)
            {
                if (impacted.Add(d))
                {
                    queue.Enqueue(d);
                }
            }
        }

        return impacted;
    }

    /// <summary>
    /// Start nodes plus every node that transitively depends on them (downstream cone).
    /// </summary>
    public static HashSet<string> CollectDownstreamClosure(
        HashSet<string> startNodeIds,
        Dictionary<string, List<string>> reverseAdjacency)
    {
        var closure = new HashSet<string>(startNodeIds, StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(startNodeIds);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!reverseAdjacency.TryGetValue(current, out var dependents))
            {
                continue;
            }

            foreach (var d in dependents)
            {
                if (closure.Add(d))
                {
                    queue.Enqueue(d);
                }
            }
        }

        return closure;
    }

    /// <summary>
    /// Topological order where every dependency appears before any dependent that references it (within <paramref name="closure"/>).
    /// </summary>
    public static TopologicalSortResult TopologicalSort(
        HashSet<string> closure,
        IReadOnlyDictionary<string, GraphNode> nodes,
        Dictionary<string, List<string>> reverseAdjacency)
    {
        var indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in closure)
        {
            indegree[id] = 0;
        }

        foreach (var id in closure)
        {
            foreach (var depId in nodes[id].Dependencies)
            {
                if (closure.Contains(depId))
                {
                    indegree[id]++;
                }
            }
        }

        var ready = new Queue<string>();
        foreach (var id in closure)
        {
            if (indegree[id] == 0)
            {
                ready.Enqueue(id);
            }
        }

        var order = new List<string>(closure.Count);
        while (ready.Count > 0)
        {
            var u = ready.Dequeue();
            order.Add(u);

            if (!reverseAdjacency.TryGetValue(u, out var dependents))
            {
                continue;
            }

            foreach (var w in dependents)
            {
                if (!closure.Contains(w))
                {
                    continue;
                }

                indegree[w]--;
                if (indegree[w] == 0)
                {
                    ready.Enqueue(w);
                }
            }
        }

        if (order.Count != closure.Count)
        {
            return new TopologicalSortResult(false, null, "Dependency cycle in the impacted subgraph; cannot produce a build order.");
        }

        return new TopologicalSortResult(true, order, null);
    }
}
