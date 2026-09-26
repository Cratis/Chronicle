// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Objects;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Extension methods for reading existing paths in an <see cref="ExpandoObject"/> without creating anything.
/// </summary>
public static class ExpandoObjectExistingPathExtensions
{
    /// <summary>
    /// Gets the existing parent of a property without creating missing objects or array elements.
    /// </summary>
    /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
    /// <param name="property">Path whose parent is requested.</param>
    /// <param name="arrayIndexers">Array indexers along the path.</param>
    /// <returns>The existing parent, or null if an ancestor or an array indexer along the path is absent.</returns>
    public static ExpandoObject? TryGetExistingPath(this ExpandoObject target, PropertyPath property, ArrayIndexers arrayIndexers)
    {
        var current = target;
        var currentPath = PropertyPath.Root;
        foreach (var segment in property.Segments.SkipLast(1))
        {
            currentPath += segment;
            var dictionary = (IDictionary<string, object?>)current;
            if (!dictionary.TryGetValue(segment.Value, out var value) || value is null)
            {
                return null;
            }

            if (segment is ArrayProperty)
            {
                if (value is not IEnumerable enumerable)
                {
                    return null;
                }

                if (!arrayIndexers.HasFor(currentPath))
                {
                    return null;
                }

                var indexer = arrayIndexers.GetFor(currentPath);
                var children = enumerable.OfType<ExpandoObject>().ToArray();
                current = !indexer.IdentifierProperty.IsSet && indexer.Identifier is int index && index >= 0 && index < children.Length
                    ? children[index]
                    : children.SingleOrDefault(child =>
                        ((IDictionary<string, object?>)child).TryGetValue(indexer.IdentifierProperty.Path, out var identifier) &&
                        identifier?.IsEqualTo(indexer.Identifier) == true);
            }
            else
            {
                current = value as ExpandoObject;
            }

            if (current is null)
            {
                return null;
            }
        }

        return current;
    }
}
