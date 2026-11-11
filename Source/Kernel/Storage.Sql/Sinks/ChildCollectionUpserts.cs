// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Upserts children into the collections held in the JSON columns of a SQL read model table.
/// </summary>
internal static class ChildCollectionUpserts
{
    /// <summary>
    /// Find the child an <see cref="ArrayIndexer"/> identifies in a list, creating it when it is missing.
    /// </summary>
    /// <param name="list">The children to search.</param>
    /// <param name="indexer">The <see cref="ArrayIndexer"/> identifying the child.</param>
    /// <returns>The child as an <see cref="ExpandoObject"/>.</returns>
    internal static ExpandoObject LocateOrCreateChild(IList<object?> list, ArrayIndexer indexer)
    {
        var identifier = ChildIdentifier.ForIndexer(indexer);

        var index = identifier.FindIndex(list, true);
        if (index >= 0)
        {
            return PromoteToExpando(list, index);
        }

        var fresh = new ExpandoObject();
        identifier.Name.SetValue(fresh, identifier.Value!, ArrayIndexers.NoIndexers);
        list.Add(fresh);
        return fresh;
    }

    /// <summary>
    /// Replace the contents of a child with a replacement document.
    /// </summary>
    /// <param name="target">The child to replace the contents of.</param>
    /// <param name="replacement">The replacement document.</param>
    internal static void ReplaceChildContents(ExpandoObject target, object? replacement)
    {
        var dict = (IDictionary<string, object?>)target;
        dict.Clear();
        if (replacement is IDictionary<string, object?> source)
        {
            CopyInto(dict, source);
        }
    }

    /// <summary>
    /// Upsert a child into a list by its identifier.
    /// </summary>
    /// <remarks>
    /// Upserting by identifier makes repeated ChildAdded events (e.g. during replay, or when a
    /// projection re-introduces the same child after a redaction) update in place instead of
    /// appending duplicates. The new child is merged into the existing entry so that any child collections
    /// populated by earlier ChildAdded events (e.g. Slices.Events) survive the replacement of
    /// the parent's own properties - a wholesale replace would clobber them.
    /// </remarks>
    /// <param name="list">The list to upsert into.</param>
    /// <param name="child">The <see cref="ChildValue"/> to upsert.</param>
    internal static void UpsertChildInList(IList<object?> list, ChildValue child)
    {
        var index = ChildIdentifier.For(child.IdentifierName, child.IdentifierValue).FindIndex(list, false);
        if (index < 0)
        {
            // Only a new child is placed by the ordering; an existing one is updated where it is and never moves.
            list.Add(AsExpando(child.NewChild));
            ChildSortPlacement.PlaceAppendedChild(list, child.Ordering, child.IdentifierName, child.ChildSchema);
            return;
        }

        if (child.NewChild is IDictionary<string, object?> incoming)
        {
            CopyInto((IDictionary<string, object?>)list[index]!, incoming);
        }
        else
        {
            list[index] = AsExpando(child.NewChild);
        }
    }

    /// <summary>
    /// Upsert a child into a collection nested in other collections, locating each parent through the indexers.
    /// </summary>
    /// <param name="parentList">The list of parents at the current level.</param>
    /// <param name="index">The index of the path segment naming the collection to descend into.</param>
    /// <param name="upsert">The <see cref="ChildUpsert"/> describing the upsert.</param>
    internal static void UpsertNestedChild(IList<object?> parentList, int index, ChildUpsert upsert)
    {
        var parentSegmentName = upsert.Segments[index - 1].Value;
        var indexer = upsert.Indexers.All.FirstOrDefault(i => i.ArrayProperty.LastSegment.Value == parentSegmentName);
        if (indexer is null)
        {
            return;
        }

        var parentDict = ChildIdentifier.ForIndexer(indexer).FindDocument(parentList);
        if (parentDict is null)
        {
            return;
        }

        var childList = GetOrCreateChildList(parentDict, upsert.Segments[index].Value);
        if (index == upsert.Segments.Length - 1)
        {
            UpsertChildInList(childList, upsert.Child);
            return;
        }

        UpsertNestedChild(childList, index + 1, upsert);
    }

    static ExpandoObject PromoteToExpando(IList<object?> list, int index)
    {
        if (list[index] is ExpandoObject existing)
        {
            return existing;
        }

        // Reify the dictionary as an ExpandoObject so EnsurePath can navigate it.
        var promoted = ToExpando((IDictionary<string, object?>)list[index]!);
        list[index] = promoted;
        return promoted;
    }

    static ExpandoObject ToExpando(IDictionary<string, object?> source)
    {
        var expando = new ExpandoObject();
        CopyInto(expando, source);
        return expando;
    }

    static void CopyInto(IDictionary<string, object?> target, IDictionary<string, object?> source)
    {
        foreach (var kvp in source)
        {
            target[kvp.Key] = kvp.Value is IDictionary<string, object?> nested ? ToExpando(nested) : kvp.Value;
        }
    }

    static object? AsExpando(object? value)
    {
        // EnsurePath walks ExpandoObject roots and filters list items via OfType<ExpandoObject>();
        // any dictionary in the list that isn't an actual ExpandoObject becomes invisible to the
        // path walker, which then creates a *second* element at the same identifier instead of
        // mutating the existing one. Promote Dictionary<string, object?> values (the form
        // UnwrapForJson produces from ExpandoObject) to ExpandoObject before they enter the list.
        return value switch
        {
            ExpandoObject => value,
            IDictionary<string, object?> dict => ToExpando(dict),
            _ => value
        };
    }

    static IList<object?> GetOrCreateChildList(IDictionary<string, object?> parent, string childCollectionName)
    {
        if (parent.TryGetValue(childCollectionName, out var existing) && existing is IList<object?> existingList)
        {
            return existingList;
        }

        var created = new List<object?>();
        parent[childCollectionName] = created;
        return created;
    }
}
