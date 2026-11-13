// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Removes children from the collections held in the JSON columns of a SQL read model table.
/// </summary>
internal static class ChildCollectionRemovals
{
    /// <summary>
    /// Remove the children described by a removal from every collection found at its path, looking through every list met on the way.
    /// </summary>
    /// <param name="node">The document node to remove from.</param>
    /// <param name="index">The index of the path segment to continue from.</param>
    /// <param name="removal">The <see cref="ChildRemoval"/> describing the children to remove.</param>
    /// <returns>True if a child was removed.</returns>
    internal static bool RemoveChildrenFromPath(object? node, int index, ChildRemoval removal)
    {
        if (index == removal.Segments.Length)
        {
            return RemoveMatchingChildren(node, removal);
        }

        if (node is IDictionary<string, object?> nested)
        {
            return nested.TryGetValue(removal.Segments[index].Value, out var value) &&
                RemoveChildrenFromPath(value, index + 1, removal);
        }

        if (node is not IList<object?> parents)
        {
            return false;
        }

        var modified = false;
        foreach (var parent in parents)
        {
            modified |= RemoveChildrenFromPath(parent, index, removal);
        }

        return modified;
    }

    /// <summary>
    /// Check whether a child is the one removed.
    /// </summary>
    /// <param name="item">The child.</param>
    /// <param name="identifier">The name of the property identifying a child.</param>
    /// <param name="key">The identifier value of the child to remove.</param>
    /// <param name="keyAsString">The key in string form.</param>
    /// <returns>True if it is the removed child, false if not.</returns>
    internal static bool IsRemovedChild(object? item, PropertyPath identifier, object? key, string? keyAsString)
    {
        if (!ChildIdentity.TryGetValue(item, identifier, out var value))
        {
            return false;
        }

        return Equals(value, key) || (value is not null && value.ToString() == keyAsString);
    }

    /// <summary>
    /// Removes the child described by <paramref name="removal"/> from its collection, selecting every ancestor array element through <paramref name="indexers"/>.
    /// </summary>
    /// <remarks>
    /// Traversal only reads: nothing is created, and a missing ancestor, indexer or collection leaves the document untouched.
    /// The terminal collection's own indexer is ignored; the victim is decided by the identifier and key.
    /// </remarks>
    /// <param name="columnValue">The deserialized JSON column holding the document.</param>
    /// <param name="removal">The collection path, identifier and key of the child to remove.</param>
    /// <param name="indexers">The indexers selecting each ancestor array element.</param>
    /// <returns>True if a child was removed.</returns>
    internal static bool RemoveNestedChild(object? columnValue, ChildRemoval removal, ArrayIndexers indexers)
    {
        if (FindChildCollection(columnValue, removal.Segments, indexers) is not IList<object?> children)
        {
            return false;
        }

        var keyAsString = removal.Key?.ToString();
        var removed = false;
        for (var position = children.Count - 1; position >= 0; position--)
        {
            if (IsRemovedChild(children[position], removal.Identifier, removal.Key, keyAsString))
            {
                children.RemoveAt(position);
                removed = true;
            }
        }

        return removed;
    }

    /// <summary>
    /// Rewrite indexers so they are relative to the element of a column, rather than the read model root.
    /// </summary>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="indexers">The indexers, carrying absolute paths from the read model root.</param>
    /// <returns>The rewritten <see cref="ArrayIndexers"/>.</returns>
    internal static ArrayIndexers RewriteIndexersRelativeTo(string columnName, ArrayIndexers indexers)
    {
        // The change's indexers carry absolute paths from the read model root (e.g.
        // "Features.Slices"). After we have descended into the column's array element we hand
        // EnsurePath a sub-context, and it walks paths relative to that element. Drop the column
        // prefix from every indexer's ArrayProperty / IdentifierProperty so they match the path
        // segments EnsurePath sees while traversing the subPath.
        var rewritten = new List<ArrayIndexer>();
        foreach (var indexer in indexers.All)
        {
            var arraySegments = indexer.ArrayProperty.Segments.ToArray();
            if (!StartsWithColumn(arraySegments, columnName))
            {
                rewritten.Add(indexer);
                continue;
            }

            // An indexer for the column itself is no longer needed inside its element.
            if (arraySegments.Length > 1)
            {
                rewritten.Add(TrimColumnPrefix(indexer, arraySegments, columnName));
            }
        }

        return new ArrayIndexers(rewritten);
    }

    static object? FindChildCollection(object? columnValue, IPropertyPathSegment[] segments, ArrayIndexers indexers)
    {
        var current = columnValue;
        for (var index = 0; index < segments.Length && current is not null; index++)
        {
            current = index == 0 ? current : ReadMember(current, segments[index].Value);
            if (index < segments.Length - 1 && segments[index] is ArrayProperty)
            {
                current = SelectAncestor(current, segments[..(index + 1)], indexers);
            }
        }

        return current;
    }

    static object? ReadMember(object? node, string name)
    {
        return node is IDictionary<string, object?> dictionary && dictionary.TryGetValue(name, out var value) ? value : null;
    }

    static IDictionary<string, object?>? SelectAncestor(object? node, IPropertyPathSegment[] arrayPath, ArrayIndexers indexers)
    {
        var indexer = indexers.All.LastOrDefault(candidate => candidate.ArrayProperty.Segments.Select(segment => segment.Value).SequenceEqual(arrayPath.Select(segment => segment.Value)));
        if (indexer is null || node is not IList<object?> elements)
        {
            return null;
        }

        if (!indexer.IdentifierProperty.IsSet)
        {
            return indexer.Identifier is int position && position >= 0 && position < elements.Count ? elements[position] as IDictionary<string, object?> : null;
        }

        return ChildIdentifier.ForIndexer(indexer).FindDocument(elements);
    }

    static bool RemoveMatchingChildren(object? node, ChildRemoval removal)
    {
        if (node is not IList<object?> children)
        {
            return false;
        }

        var removed = false;
        for (var childIndex = children.Count - 1; childIndex >= 0; childIndex--)
        {
            if (IsMatchingChild(children[childIndex], removal))
            {
                children.RemoveAt(childIndex);
                removed = true;
            }
        }

        return removed;
    }

    static bool IsMatchingChild(object? child, ChildRemoval removal) =>
        ChildIdentity.TryGetValue(child, removal.Identifier, out var value) &&
        JsonColumnSearch.CompareLoose(value, removal.Key);

    static bool StartsWithColumn(IPropertyPathSegment[] segments, string columnName) =>
        segments.Length > 0 && string.Equals(segments[0].Value, columnName, StringComparison.Ordinal);

    static ArrayIndexer TrimColumnPrefix(ArrayIndexer indexer, IPropertyPathSegment[] arraySegments, string columnName)
    {
        var trimmedArray = PropertyPath.CreateFrom(arraySegments.Skip(1).ToArray());
        var identifierSegments = indexer.IdentifierProperty.Segments.ToArray();
        var trimmedIdentifier = StartsWithColumn(identifierSegments, columnName)
            ? PropertyPath.CreateFrom(identifierSegments.Skip(1).ToArray())
            : indexer.IdentifierProperty;

        // Normalize identifiers to string form. JSON round-trips turn Guid identifiers into
        // strings and strip ConceptAs<T> wrappers, so the inside-EnsurePath comparison of the
        // stored value (string) against the original Identifier (concept/Guid) never matches
        // and manufactures a duplicate entry instead of finding the existing one. Comparing
        // the two as strings makes the lookup deterministic across types.
        var normalizedIdentifier = JsonColumnDocuments.UnwrapConceptValue(indexer.Identifier)?.ToString();
        return new ArrayIndexer(trimmedArray, trimmedIdentifier, normalizedIdentifier!);
    }
}
