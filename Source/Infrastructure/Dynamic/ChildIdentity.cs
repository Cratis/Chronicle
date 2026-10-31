// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Resolves child identities in dynamic and deserialized documents without creating missing members.
/// </summary>
internal static class ChildIdentity
{
    /// <summary>
    /// Reads the complete identity path from a child document.
    /// </summary>
    /// <param name="child">The child document.</param>
    /// <param name="path">The identity path relative to the child.</param>
    /// <param name="value">The identity value, when present.</param>
    /// <returns>Whether every segment was present.</returns>
    internal static bool TryGetValue(object? child, PropertyPath path, out object? value)
    {
        value = child;
        foreach (var segment in path.Segments)
        {
            if (value is not IDictionary<string, object?> dictionary || !dictionary.TryGetValue(segment.Value, out value))
            {
                value = null;
                return false;
            }
        }

        return true;
    }
}
