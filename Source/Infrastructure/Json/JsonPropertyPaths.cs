// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Json;

/// <summary>
/// Reads and writes a JSON object through a dotted property path.
/// </summary>
/// <remarks>
/// A property path addresses a value by walking objects segment by segment, so <c language="csharp">Price.Description</c> means the
/// <c language="csharp">Description</c> of the <c language="csharp">Price</c> object rather than a top-level property whose name contains a dot. Event
/// type migrations have always produced such paths from a nested property expression; the kernel read and wrote
/// them flat, so a nested migration either failed validation or silently wrote a top-level key with a dot in its
/// name that the target generation's schema then discarded (#3949).
/// </remarks>
public static class JsonPropertyPaths
{
    const char Separator = '.';

    /// <summary>
    /// Resolves the value a dotted property path addresses.
    /// </summary>
    /// <param name="root">The <see cref="JsonObject"/> to resolve within.</param>
    /// <param name="path">The dotted property path.</param>
    /// <param name="value">When this returns <see langword="true"/>, the value the path addresses - which may itself be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when every segment of the path resolved; otherwise <see langword="false"/>.</returns>
    public static bool TryResolve(JsonObject root, string path, out JsonNode? value)
    {
        value = null;

        var segments = Split(path);
        JsonNode? current = root;

        for (var index = 0; index < segments.Length; index++)
        {
            if (current is not JsonObject currentObject ||
                !currentObject.TryGetPropertyValue(segments[index], out var next))
            {
                return false;
            }

            current = next;
        }

        value = current;
        return true;
    }

    /// <summary>
    /// Writes a value at the dotted property path, creating the objects along the way.
    /// </summary>
    /// <param name="root">The <see cref="JsonObject"/> to write into.</param>
    /// <param name="path">The dotted property path.</param>
    /// <param name="value">The value to write, which may be <see langword="null"/>.</param>
    /// <remarks>
    /// A segment that is missing, or that holds something other than an object, is replaced by a fresh object so the
    /// remaining segments have somewhere to live. Writing into a payload whose nested object has not been created
    /// yet - which is exactly what a migration adding a value inside one does - is the normal case, not an error.
    /// </remarks>
    public static void Set(JsonObject root, string path, JsonNode? value)
    {
        var segments = Split(path);
        var current = root;

        for (var index = 0; index < segments.Length - 1; index++)
        {
            var segment = segments[index];
            if (current[segment] is not JsonObject next)
            {
                next = [];
                current[segment] = next;
            }

            current = next;
        }

        current[segments[^1]] = value;
    }

    /// <summary>
    /// Splits a dotted property path into its segments.
    /// </summary>
    /// <param name="path">The dotted property path.</param>
    /// <returns>The segments of the path.</returns>
    public static string[] Split(string path) => path.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
}
