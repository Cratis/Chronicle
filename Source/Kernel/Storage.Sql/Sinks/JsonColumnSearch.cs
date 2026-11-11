// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Reflection;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Searches the loosely typed documents held in the JSON columns of a SQL read model table, comparing values as loosely as the stored form requires.
/// </summary>
internal static class JsonColumnSearch
{
    /// <summary>
    /// Find a value at a path in a deserialized document, looking through every element of a list met on the way.
    /// </summary>
    /// <param name="node">The document node to search.</param>
    /// <param name="segments">The segments of the path to the value.</param>
    /// <param name="index">The index of the segment to continue from.</param>
    /// <param name="target">The value to find.</param>
    /// <returns>True if the value is found, false if not.</returns>
    internal static bool TryFindValueInDeserialized(object? node, IPropertyPathSegment[] segments, int index, object target)
    {
        if (node is null || index >= segments.Length)
        {
            return false;
        }

        return node switch
        {
            IList<object?> list => list.Any(item => TryFindValueInDeserialized(item, segments, index, target)),
            IDictionary<string, object?> dict => TryFindValueInDictionary(dict, segments, index, target),
            _ => index == segments.Length - 1 && CompareLoose(node, target)
        };
    }

    /// <summary>
    /// Compare two values as equal when they are equal, equal as concepts, or equal as text.
    /// </summary>
    /// <param name="value">The stored value.</param>
    /// <param name="target">The value to compare with.</param>
    /// <returns>True if they match, false if not.</returns>
    internal static bool CompareLoose(object? value, object? target)
    {
        if (value is null || target is null)
        {
            return value is null && target is null;
        }

        if (value.Equals(target) || MatchesConceptValue(value, target))
        {
            return true;
        }

        return string.Equals(value.ToString(), target.ToString(), StringComparison.Ordinal);
    }

    static bool TryFindValueInDictionary(IDictionary<string, object?> dict, IPropertyPathSegment[] segments, int index, object target)
    {
        if (!dict.TryGetValue(segments[index].Value, out var value))
        {
            return false;
        }

        return index == segments.Length - 1
            ? CompareLoose(value, target)
            : TryFindValueInDeserialized(value, segments, index + 1, target);
    }

    static bool MatchesConceptValue(object value, object target) =>
        (target.IsConcept() && value.Equals(target.GetConceptValue())) ||
        (value.IsConcept() && (value.GetConceptValue()?.Equals(target) ?? false));
}
