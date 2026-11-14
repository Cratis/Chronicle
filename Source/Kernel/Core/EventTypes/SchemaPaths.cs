// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Resolves dotted property paths against a <see cref="JsonSchema"/>.
/// </summary>
static class SchemaPaths
{
    /// <summary>
    /// Gets whether a schema declares the property a dotted path addresses.
    /// </summary>
    /// <param name="schema">The <see cref="JsonSchema"/> to resolve within.</param>
    /// <param name="path">The dotted property path a migration carries.</param>
    /// <returns><see langword="true"/> when every segment of the path is declared; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// A migration built from a nested property expression carries a path rather than a name, so resolving it
    /// against the top-level property map alone reported that the property did not exist when it existed one level
    /// down (#3949).
    /// </remarks>
    internal static bool Declares(JsonSchema schema, string path)
    {
        var current = schema;

        foreach (var segment in JsonPropertyPaths.Split(path))
        {
            if (!TryResolve(current, segment, out current))
            {
                return false;
            }
        }

        return true;
    }

    static bool TryResolve(JsonSchema schema, string segment, out JsonSchema resolved)
    {
        resolved = schema;
        var addressesElements = segment.EndsWith(JsonPropertyPaths.ElementMarker, StringComparison.Ordinal);
        var name = addressesElements ? segment[..^JsonPropertyPaths.ElementMarker.Length] : segment;

        if (!schema.ActualProperties.TryGetValue(name, out var property))
        {
            return false;
        }

        resolved = property.ActualSchema;
        if (!addressesElements)
        {
            return true;
        }

        if (resolved.Item is null)
        {
            return false;
        }

        resolved = resolved.Item.ActualSchema;
        return true;
    }
}
