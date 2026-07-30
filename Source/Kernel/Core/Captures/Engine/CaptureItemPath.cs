// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Provides resolution of dotted property paths within captured items.
/// </summary>
public static class CaptureItemPath
{
    /// <summary>
    /// Resolve the value at a dotted property path within an item.
    /// </summary>
    /// <param name="item">The item to resolve within - null yields null.</param>
    /// <param name="path">The dotted property path.</param>
    /// <returns>The <see cref="JsonNode"/> at the path, or null when any segment is missing.</returns>
    public static JsonNode? Resolve(JsonObject? item, string path)
    {
        JsonNode? currentNode = item;
        foreach (var segment in path.Split('.'))
        {
            if (currentNode is not JsonObject currentObject || !currentObject.TryGetPropertyValue(segment, out var next))
            {
                return null;
            }

            currentNode = next;
        }

        return currentNode;
    }
}
