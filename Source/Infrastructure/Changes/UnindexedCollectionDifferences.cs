// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Extension methods for collapsing property differences that target an element inside an unindexed
/// collection into a single whole-collection replacement.
/// </summary>
/// <remarks>
/// When PII is re-encrypted across a whole read-model snapshot, the object comparer reports a difference
/// for each nested child member (e.g. <c>contacts.contactEmail</c>) but has no child identity to attach,
/// so the difference carries no array indexers. A sink cannot apply such a nested, unindexed array path —
/// MongoDB rejects the dotted <c>$set</c> with WriteError Code 28. Both the reducer and the projection
/// encryption step rely on this collapse to replace the entire collection in one safe <c>$set</c> instead.
/// </remarks>
public static class UnindexedCollectionDifferences
{
    /// <summary>
    /// Collapses every difference that targets an element inside an unindexed collection into a single
    /// difference that replaces the whole collection, leaving all other differences untouched.
    /// </summary>
    /// <param name="differences">The differences to collapse.</param>
    /// <param name="original">The original state the differences were computed from.</param>
    /// <param name="changed">The changed state the differences were computed against.</param>
    /// <returns>The collapsed differences.</returns>
    public static IReadOnlyList<PropertyDifference> Collapse(
        this IEnumerable<PropertyDifference> differences,
        ExpandoObject? original,
        ExpandoObject changed)
    {
        var result = new List<PropertyDifference>();
        if (differences is null)
        {
            return result;
        }

        var collapsed = new HashSet<(PropertyPath PropertyPath, ArrayIndexers ArrayIndexers)>();

        foreach (var difference in differences)
        {
            if (!TryGetUnindexedCollectionPath(difference, out var collectionPath))
            {
                result.Add(difference);
                continue;
            }

            var collapsedKey = (collectionPath, difference.ArrayIndexers);
            if (!collapsed.Add(collapsedKey))
            {
                continue;
            }

            result.Add(new PropertyDifference(
                collectionPath,
                GetValueAtPath(original, collectionPath, difference.ArrayIndexers),
                GetValueAtPath(changed, collectionPath, difference.ArrayIndexers),
                difference.ArrayIndexers));
        }

        return result;
    }

    static bool TryGetUnindexedCollectionPath(PropertyDifference difference, out PropertyPath collectionPath)
    {
        collectionPath = PropertyPath.NotSet;

        var currentPath = PropertyPath.Root;
        var segments = difference.PropertyPath.Segments.ToArray();

        for (var segmentIndex = 0; segmentIndex < segments.Length - 1; segmentIndex++)
        {
            var segment = segments[segmentIndex];
            currentPath += segment;

            if (segment is ArrayProperty && !difference.ArrayIndexers.HasFor(currentPath))
            {
                collectionPath = currentPath;
                return true;
            }
        }

        return false;
    }

    static object? GetValueAtPath(ExpandoObject? instance, PropertyPath propertyPath, ArrayIndexers arrayIndexers)
    {
        object? current = instance;
        var currentPath = PropertyPath.Root;

        foreach (var segment in propertyPath.Segments)
        {
            if (current is not ExpandoObject expandoObject)
            {
                return null;
            }

            if (!(expandoObject as IDictionary<string, object?>).TryGetValue(segment.Value, out current))
            {
                return null;
            }

            currentPath += segment;

            if (segment is ArrayProperty && arrayIndexers.HasFor(currentPath))
            {
                current = GetElementForIndexer(current, arrayIndexers.GetFor(currentPath));
            }
        }

        return current;
    }

    static object? GetElementForIndexer(object? collection, ArrayIndexer indexer)
    {
        if (collection is not IEnumerable enumerable)
        {
            return null;
        }

        var elements = enumerable.Cast<object>().ToArray();
        if (!indexer.IdentifierProperty.IsSet && indexer.Identifier is int index)
        {
            return elements.Length > index ? elements[index] : null;
        }

        return elements
            .OfType<ExpandoObject>()
            .Cast<IDictionary<string, object?>>()
            .SingleOrDefault(_ =>
                _.TryGetValue(indexer.IdentifierProperty.Path, out var identifier) &&
                Equals(identifier, indexer.Identifier));
    }
}
