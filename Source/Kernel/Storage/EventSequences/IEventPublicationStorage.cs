// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
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
    /// Looks up several durable identities without comparing their fingerprints.
    /// </summary>
    /// <param name="identities">The identities to look up.</param>
    /// <returns>The stored sequence number and fingerprint per identity found; absent identities are omitted.</returns>
    Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(IEnumerable<string> identities);

    /// <summary>
    /// Appends a batch atomically, including each event's optional identity and fingerprint.
    /// </summary>
    /// <param name="events">The validated and protected events to append.</param>
    /// <returns>The appended events, a slot collision, or existing identity receipts. A failure writes none of the batch.</returns>
    /// <exception cref="EventsNotCommitted">The provider proved nothing from the attempt was committed.</exception>
    /// <remarks>Other exceptions may have an unknown commit outcome and must not be treated as proof of absence.</remarks>
    Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(IEnumerable<EventToAppendToStorage> events);

    /// <summary>
    /// Atomically appends an event and its publication identity, or returns its existing receipt.
    /// </summary>
    /// <param name="publication">The immutable publication intent identity and fingerprint.</param>
    /// <param name="event">The validated and protected event to append.</param>
    /// <returns>A durable receipt, or the existing sequence-slot conflict outcome.</returns>
    /// <exception cref="EventPublicationConflict">The identity was reused for a different intent.</exception>
    Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(EventPublication publication, EventToAppendToStorage @event);
}
