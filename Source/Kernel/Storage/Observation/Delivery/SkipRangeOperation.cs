// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents an immutable S row operation. Upsert is not supported.
/// </summary>
public sealed record SkipRangeOperation
{
    SkipRangeOperation(DeliveryRowOperationKind kind, DeliveryGeneration generation, SequencingKeyHash keyHash, EventSequenceNumber from, SkipRange? row)
    {
        Kind = kind;
        Generation = generation;
        KeyHash = keyHash;
        From = from;
        Row = row;
    }

    /// <summary>
    /// Gets the strict insert or idempotent delete semantics.
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
    /// Gets the inclusive range start identifying the row.
    /// </summary>
    public EventSequenceNumber From { get; }

    /// <summary>
    /// Gets the complete insert row, or null for an identity-only delete.
    /// </summary>
    public SkipRange? Row { get; }

    /// <summary>
    /// Creates a strict insert.
    /// </summary>
    /// <param name="row">The complete row.</param>
    /// <returns>The insert operation.</returns>
    public static SkipRangeOperation Insert(SkipRange row) => new(DeliveryRowOperationKind.Insert, row.Generation, row.Key.Hash, row.From, row);

    /// <summary>
    /// Creates an idempotent delete guarded by the root CAS, without a row revision.
    /// </summary>
    /// <param name="generation">The row generation.</param>
    /// <param name="keyHash">The row key hash.</param>
    /// <param name="from">The inclusive range start.</param>
    /// <returns>The delete operation.</returns>
    public static SkipRangeOperation Delete(DeliveryGeneration generation, SequencingKeyHash keyHash, EventSequenceNumber from) =>
        new(DeliveryRowOperationKind.Delete, generation, keyHash, from, null);
}
