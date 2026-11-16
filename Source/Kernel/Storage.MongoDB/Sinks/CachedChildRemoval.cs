// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Removes a child from cached parent state without discarding pending bulk changes.
/// </summary>
internal static class CachedChildRemoval
{
    /// <summary>
    /// Removes the identified child from every matching collection in a cached document.
    /// </summary>
    /// <param name="state">The cached parent state to update in place.</param>
    /// <param name="childRemoved">The child and collection path to remove from.</param>
    internal static void Remove(ExpandoObject state, ChildRemovedFromAll childRemoved)
    {
        var segments = childRemoved.ChildrenProperty.Segments.Select(_ => _.Value).ToArray();
        VisitPath(state, segments, 0, childRemoved);
    }

    static void VisitPath(IDictionary<string, object?> document, string[] segments, int index, ChildRemovedFromAll childRemoved)
    {
        if (index >= segments.Length)
        {
            return;
        }

        if (!document.TryGetValue(segments[index], out var value) || value is null)
        {
            return;
        }

        if (index == segments.Length - 1)
        {
            VisitChildCollection(value, childRemoved);
            return;
        }

        if (value is IDictionary<string, object?> nested)
        {
            VisitPath(nested, segments, index + 1, childRemoved);
        }
        else if (value is IEnumerable collection)
        {
            foreach (var item in collection.OfType<IDictionary<string, object?>>())
            {
                VisitPath(item, segments, index + 1, childRemoved);
            }
        }
    }

    static void VisitChildCollection(object value, ChildRemovedFromAll childRemoved)
    {
        if (value is IList children && children.Cast<object>().FindByKey(childRemoved.IdentifiedByProperty, childRemoved.Key) is { } child)
        {
            children.Remove(child);
        }
    }
}
