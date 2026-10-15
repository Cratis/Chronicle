// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents an immutable atomic root CAS and its F and S row operations.
/// </summary>
/// <param name="Expected">The expected root revision, or None for creation.</param>
/// <param name="Root">The replacement root.</param>
/// <param name="ParkedKeys">F operations, copied at construction.</param>
/// <param name="SkipRanges">S operations, copied at construction.</param>
public sealed record DeliveryCommit(
    DeliveryRevision Expected,
    ObserverDeliveryRoot Root,
    ImmutableArray<ParkedKeyOperation> ParkedKeys,
    ImmutableArray<SkipRangeOperation> SkipRanges)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryCommit"/> class, copying mutable source collections.
    /// </summary>
    /// <param name="expected">The expected root revision.</param>
    /// <param name="root">The replacement root.</param>
    /// <param name="parkedKeys">The source F operations.</param>
    /// <param name="skipRanges">The source S operations.</param>
    public DeliveryCommit(DeliveryRevision expected, ObserverDeliveryRoot root, IEnumerable<ParkedKeyOperation> parkedKeys, IEnumerable<SkipRangeOperation> skipRanges)
        : this(expected, root, [.. parkedKeys], [.. skipRanges])
    {
    }

    /// <summary>
    /// Gets the immutable F operations.
    /// </summary>
    public ImmutableArray<ParkedKeyOperation> ParkedKeys { get; } = DeliveryCollections.Copy(ParkedKeys);

    /// <summary>
    /// Gets the immutable S operations.
    /// </summary>
    public ImmutableArray<SkipRangeOperation> SkipRanges { get; } = DeliveryCollections.Copy(SkipRanges);

    /// <inheritdoc/>
    public bool Equals(DeliveryCommit? other) => other is not null && Expected == other.Expected && Root == other.Root &&
        ParkedKeys.SequenceEqual(other.ParkedKeys) && SkipRanges.SequenceEqual(other.SkipRanges);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Expected, Root, DeliveryCollections.Hash(ParkedKeys), DeliveryCollections.Hash(SkipRanges));
}
