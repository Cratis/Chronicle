// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents an immutable F row operation. Only insert and upsert carry a row revision.
/// </summary>
public sealed record ParkedKeyOperation
{
    ParkedKeyOperation(DeliveryRowOperationKind kind, DeliveryGeneration generation, SequencingKeyHash keyHash, ParkedKey? row)
    {
        Kind = kind;
        Generation = generation;
        KeyHash = keyHash;
        Row = row;
    }

    /// <summary>
    /// Gets the operation semantics.
    /// </summary>
    public DeliveryRowOperationKind Kind { get; }

    /// <summary>
    /// Gets the row generation.
    /// </summary>
    public DeliveryGeneration Generation { get; }

    /// <summary>
    /// Gets the row key hash.
    /// </summary>
    public SequencingKeyHash KeyHash { get; }

    /// <summary>
    /// Gets the complete insert or upsert row, or null for an identity-only delete.
    /// </summary>
    public ParkedKey? Row { get; }

    /// <summary>
    /// Creates a strict insert.
    /// </summary>
    /// <param name="row">The complete row.</param>
    /// <returns>The insert operation.</returns>
    public static ParkedKeyOperation Insert(ParkedKey row) => new(DeliveryRowOperationKind.Insert, row.Generation, row.Key.Hash, row);

    /// <summary>
    /// Creates an upsert.
    /// </summary>
    /// <param name="row">The complete row.</param>
    /// <returns>The upsert operation.</returns>
    public static ParkedKeyOperation Upsert(ParkedKey row) => new(DeliveryRowOperationKind.Upsert, row.Generation, row.Key.Hash, row);

    /// <summary>
    /// Creates an idempotent delete guarded by the root CAS, without a row revision.
    /// </summary>
    /// <param name="generation">The row generation.</param>
    /// <param name="keyHash">The row key hash.</param>
    /// <returns>The delete operation.</returns>
    public static ParkedKeyOperation Delete(DeliveryGeneration generation, SequencingKeyHash keyHash) => new(DeliveryRowOperationKind.Delete, generation, keyHash, null);
}
