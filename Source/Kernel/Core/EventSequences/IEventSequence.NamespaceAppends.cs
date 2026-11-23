// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences;

public partial interface IEventSequence
{
    /// <summary>Appends one event only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="correlationId">The correlation identity.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity causing the event.</param>
    /// <param name="concurrencyScope">The concurrency check for this append.</param>
    /// <returns>The confirmed append result.</returns>
    Task<AppendResult> AppendToNamespace(NamespaceIncarnationId expectedIncarnation, EventToAppend @event, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScope concurrencyScope);

    /// <summary>Appends a batch only in the caller's expected namespace incarnation.</summary>
    /// <param name="expectedIncarnation">The caller's fixed namespace identity, retained across retries.</param>
    /// <param name="events">The events to append.</param>
    /// <param name="correlationId">The correlation identity.</param>
    /// <param name="causation">The causation chain.</param>
    /// <param name="causedBy">The identity causing the events.</param>
    /// <param name="concurrencyScopes">The concurrency checks for the batch.</param>
    /// <returns>The confirmed batch result.</returns>
    Task<AppendManyResult> AppendManyToNamespace(NamespaceIncarnationId expectedIncarnation, IEnumerable<EventToAppend> events, CorrelationId correlationId, IEnumerable<Causation> causation, Identity causedBy, ConcurrencyScopes concurrencyScopes);
}
