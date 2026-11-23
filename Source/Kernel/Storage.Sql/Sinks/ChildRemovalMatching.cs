// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Matches the identity of a child being removed without changing scalar comparison semantics.
/// </summary>
internal static class ChildRemovalMatching
{
    /// <summary>
    /// Matches a removal identity using structural equality for composite keys and existing scalar comparisons otherwise.
    /// </summary>
    /// <param name="value">The stored identity.</param>
    /// <param name="key">The identity to remove.</param>
    /// <param name="keyAsString">The removal identity in string form.</param>
    /// <returns>Whether the stored identity is the one to remove.</returns>
    internal static bool Matches(object? value, object? key, string? keyAsString)
    {
        if (ChildKeyComparison.RequiresStructuralComparison(value, key))
        {
            return ChildKeyComparison.AreEqual(value, key);
        }

        return Equals(value, key) || (value is not null && value.ToString() == keyAsString);
    }
}
