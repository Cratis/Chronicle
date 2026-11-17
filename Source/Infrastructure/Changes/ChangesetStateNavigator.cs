// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Reads values at property paths from a state, through expando objects, plain objects and array indexers.
/// </summary>
internal static class ChangesetStateNavigator
{
    /// <summary>
    /// Try to get the value found by following a property path from an instance.
    /// </summary>
    /// <param name="instance">The instance to start from.</param>
    /// <param name="propertyPath">The path to follow.</param>
    /// <param name="arrayIndexers">Indexers selecting elements of the arrays on the path.</param>
    /// <param name="value">The value found when the path could be followed.</param>
    /// <returns>True if the whole path could be followed, false if not.</returns>
    internal static bool TryGetValueAtPath(object? instance, PropertyPath propertyPath, ArrayIndexers arrayIndexers, out object? value)
    {
        value = instance;
        var currentPath = PropertyPath.Root;

        foreach (var segment in propertyPath.Segments)
        {
            if (!TryGetMember(ref value, segment))
            {
                return false;
            }

            currentPath += segment;
            if (segment is ArrayProperty && arrayIndexers.HasFor(currentPath))
            {
                value = GetElementForIndexer(value, arrayIndexers.GetFor(currentPath));
                if (value is null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    static bool TryGetMember(ref object? value, IPropertyPathSegment segment)
    {
        if (value is ExpandoObject expandoObject)
        {
            return ((IDictionary<string, object?>)expandoObject).TryGetValue(segment.Value, out value);
        }

        var propertyInfo = value?.GetType().GetProperties().SingleOrDefault(_ => _.Name.Equals(segment.Value, StringComparison.OrdinalIgnoreCase));
        if (propertyInfo is null)
        {
            return false;
        }

        value = propertyInfo.GetValue(value);
        return true;
    }

    static object? GetElementForIndexer(object? collection, ArrayIndexer indexer)
    {
        if (collection is not IEnumerable enumerable)
        {
            return null;
        }

        if (!indexer.IdentifierProperty.IsSet && indexer.Identifier is int index)
        {
            var elements = enumerable.Cast<object>().ToArray();
            return elements.Length > index ? elements[index] : null;
        }

        return enumerable.Cast<object>()
            .OfType<ExpandoObject>()
            .Cast<IDictionary<string, object?>>()
            .SingleOrDefault(_ =>
                ChildIdentity.TryGetValue(_, indexer.IdentifierProperty, out var identifier) &&
                Equals(identifier, indexer.Identifier));
    }
}
