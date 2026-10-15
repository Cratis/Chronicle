// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Copies and hashes immutable delivery collections by content.
/// </summary>
static class DeliveryCollections
{
    /// <summary>
    /// Copies a collection into independently owned storage.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The source items.</param>
    /// <returns>The copied items, or empty for a default array.</returns>
    public static ImmutableArray<T> Copy<T>(ImmutableArray<T> items) => items.IsDefault ? [] : [.. items.AsSpan()];

    /// <summary>
    /// Computes an ordered content hash.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The items to hash.</param>
    /// <returns>The content hash.</returns>
    public static int Hash<T>(ImmutableArray<T> items)
    {
        var hash = default(HashCode);
        foreach (var item in items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }
}
