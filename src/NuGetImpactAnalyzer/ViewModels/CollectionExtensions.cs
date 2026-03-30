using System.Collections.ObjectModel;

namespace NuGetImpactAnalyzer.ViewModels;

/// <summary>
/// View-model helpers for updating bound collections without duplicating clear/add loops.
/// </summary>
internal static class CollectionExtensions
{
    public static void ReplaceAll<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
