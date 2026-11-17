// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Finds the parents of a property that are absent, or null, in the initial state.
/// </summary>
internal static class MissingParents
{
    /// <summary>
    /// Find the parents of a property that need to be created before the property can be written.
    /// </summary>
    /// <param name="initialState">The initial state to look the parents up in, or null to treat every parent as missing.</param>
    /// <param name="path">The <see cref="PropertyPath"/> of the property.</param>
    /// <param name="indexers">The <see cref="ArrayIndexers"/> selecting the children the property is under.</param>
    /// <returns>The <see cref="PropertyPath"/> of every missing parent, outermost first.</returns>
    public static IEnumerable<PropertyPath> Find(ExpandoObject? initialState, PropertyPath path, ArrayIndexers indexers)
    {
        // Identifier-less indexers cannot safely select a child for a pre-unset: their MongoDB
        // filter has no stable identity, and a numeric path can shift between reading and writing.
        // Leave their leaf update on the existing path rather than risk unsetting the wrong child.
        if (indexers.All.Any(_ => !_.IdentifierProperty.IsSet))
        {
            yield break;
        }

        var segments = path.Segments.ToArray();
        var parentPath = PropertyPath.Root;
        for (var index = 0; index < segments.Length - 1; index++)
        {
            parentPath += segments[index];
            if (segments[index] is ArrayProperty)
            {
                // Only indexed elements can be repaired without changing their siblings.
                if (!indexers.HasFor(parentPath))
                {
                    yield break;
                }

                continue;
            }

            if (IsMissing(initialState, parentPath, segments[index], indexers))
            {
                yield return parentPath;
            }
        }
    }

    static bool IsMissing(ExpandoObject? initialState, PropertyPath parentPath, IPropertyPathSegment segment, ArrayIndexers indexers)
    {
        // Look up the actual identified child in the initial state. A parent already present
        // on that child needs no pre-unset; joins still use a null initial state because their
        // matched documents can have different shapes.
        var existingParent = initialState?.TryGetExistingPath(parentPath, indexers);
        return existingParent is null ||
            !((IDictionary<string, object?>)existingParent).TryGetValue(segment.Value, out var value) ||
            value is null;
    }
}
