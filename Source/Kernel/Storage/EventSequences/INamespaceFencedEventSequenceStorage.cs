// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Optional capability for committing validated events under an exact namespace incarnation fence.
/// </summary>
/// <remarks>
/// Every operation, including an empty batch or collision recovery, must acquire the marker fence and
/// compare the activation incarnation before touching events. Unknown commits must not be reported as aborted.
/// This capability does not replace the tokenless storage interfaces or issue namespace identities.
/// </remarks>
public interface INamespaceFencedEventSequenceStorage
{
    /// <summary>
    /// Atomically appends a single event after validating the supplied namespace and activation identities.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="event">The validated event to append; optional publication identity is ignored.</param>
    /// <returns>The appended event or a sequence-slot collision.</returns>
    Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(NamespaceCommitContext context, EventToAppendToStorage @event);

    /// <summary>
    /// Atomically appends a batch after validating the supplied namespace and activation identities.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="events">The validated events to append; optional publication identities are ignored.</param>
    /// <returns>The appended events or a sequence-slot collision.</returns>
    Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendMany(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events);

    /// <summary>
    /// Atomically appends events with their optional publication identities under the namespace fence.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="events">The validated events with optional immutable identities and fingerprints.</param>
    /// <returns>The appended events, a sequence-slot collision, or fenced durable identity receipts.</returns>
    Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendManyWithIdentities(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events);

    /// <summary>
    /// Looks up a publication under the namespace fence, rejecting a different fingerprint.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="publication">The immutable publication identity and fingerprint.</param>
    /// <returns>The fenced durable receipt, if present.</returns>
    Task<Option<EventPublicationReceipt>> TryGetPublication(NamespaceCommitContext context, EventPublication publication);

    /// <summary>
    /// Looks up publication identities under the namespace fence without comparing fingerprints.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="identities">The identities to look up.</param>
    /// <returns>The stored sequence number and fingerprint per identity found.</returns>
    Task<IReadOnlyDictionary<string, StoredEventPublication>> TryGetPublications(NamespaceCommitContext context, IEnumerable<string> identities);

    /// <summary>
    /// Atomically appends a publication or settles its existing receipt under the namespace fence.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="publication">The immutable publication identity and fingerprint.</param>
    /// <param name="event">The validated and protected event to append.</param>
    /// <returns>The fenced receipt or a sequence-slot collision.</returns>
    Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(NamespaceCommitContext context, EventPublication publication, EventToAppendToStorage @event);

    /// <summary>
    /// Looks up an effect receipt under the namespace fence, independently of its destination sequence.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="id">The receipt identity.</param>
    /// <returns>The fenced durable receipt, if present.</returns>
    Task<EffectReceipt?> GetEffectReceipt(NamespaceCommitContext context, string id);

    /// <summary>
    /// Reads an occurrence's effect receipts under the namespace fence across destination sequences.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="occurrenceId">The occurrence identity.</param>
    /// <returns>The fenced durable receipts.</returns>
    Task<IEnumerable<EffectReceipt>> GetEffectReceipts(NamespaceCommitContext context, OccurrenceId occurrenceId);

    /// <summary>
    /// Reads effect receipts for occurrences under the namespace fence across destination sequences.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="occurrenceIds">The occurrence identities.</param>
    /// <returns>The fenced durable receipts.</returns>
    Task<IEnumerable<EffectReceipt>> GetEffectReceiptsForOccurrences(NamespaceCommitContext context, IEnumerable<OccurrenceId> occurrenceIds);

    /// <summary>
    /// Atomically appends a batch and effect receipt, or settles its replay, under the namespace fence.
    /// </summary>
    /// <param name="context">The expected namespace identity and captured activation state/lifetime.</param>
    /// <param name="events">The validated and protected events; may be empty.</param>
    /// <param name="receipt">The immutable receipt to persist.</param>
    /// <returns>The fenced durable storage outcome.</returns>
    Task<EffectReceiptWriteResult> AppendManyWithEffectReceipt(NamespaceCommitContext context, IEnumerable<EventToAppendToStorage> events, EffectReceipt receipt);
}
