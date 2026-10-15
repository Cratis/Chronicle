// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents an immutable parked key page; the next cursor comes from the last item.
/// </summary>
/// <param name="Items">The page items, copied at construction.</param>
/// <param name="HasMore">Whether another page exists.</param>
public sealed record ParkedKeyPage(ImmutableArray<ParkedKey> Items, bool HasMore)
{
    /// <summary>
    /// Gets the immutable page items.
    /// </summary>
    public ImmutableArray<ParkedKey> Items { get; } = DeliveryCollections.Copy(Items);

    /// <inheritdoc/>
    public bool Equals(ParkedKeyPage? other) => other is not null && HasMore == other.HasMore && Items.SequenceEqual(other.Items);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(HasMore, DeliveryCollections.Hash(Items));
}
