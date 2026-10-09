// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Owns atomic publication deduplication within one store, namespace and destination sequence.
/// </summary>
/// <remarks>
/// The publication identity and fingerprint must be persisted in the same atomic write as the event.
/// A receipt survives lost responses and provider recreation. Sequence-slot collisions are separate
/// from publication retries. This contract accepts only already validated, migrated and protected events;
/// callers must use the event sequence's append pipeline, not bypass it.
/// </remarks>
public interface IEventPublicationStorage
{
    /// <summary>
    /// Looks up a durable publication receipt, rejecting reuse for a different intent.
    /// </summary>
    /// <param name="publication">The immutable publication intent identity and fingerprint.</param>
    /// <returns>The receipt if the event is durable.</returns>
    Task<Option<EventPublicationReceipt>> TryGetPublication(EventPublication publication);

    /// <summary>
    /// Atomically appends an event and its publication identity, or returns its existing receipt.
    /// </summary>
    /// <param name="publication">The immutable publication intent identity and fingerprint.</param>
    /// <param name="event">The validated and protected event to append.</param>
    /// <returns>A durable receipt, or the existing sequence-slot conflict outcome.</returns>
    /// <exception cref="EventPublicationConflict">The identity was reused for a different intent.</exception>
    Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(EventPublication publication, EventToAppendToStorage @event);
}
