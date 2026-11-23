// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Namespaces;
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
}
