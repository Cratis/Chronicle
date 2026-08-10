// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an ordered collection of events to append together with the concurrency scopes that must be checked
/// by the same append operation.
/// </summary>
/// <remarks>
/// Both inputs are materialized when this value is created. A missing scope or a
/// <see cref="ConcurrencyScope.NotSet"/> scope retains the normal <see cref="IEventSequence.AppendMany(IEnumerable{EventForEventSourceId}, CorrelationId?, IEnumerable{string}?, IDictionary{EventSourceId, ConcurrencyScope}?)"/>
/// behavior and is resolved by the configured concurrency strategy. Use <see cref="ConcurrencyScope.None"/> to
/// explicitly append without a concurrency check for a key.
/// </remarks>
public sealed class EventsWithConcurrencyScopes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventsWithConcurrencyScopes"/> class.
    /// </summary>
    /// <param name="events">The events to append, in append order.</param>
    /// <param name="concurrencyScopes">The source-keyed concurrency scopes to pass to the append operation.</param>
    /// <exception cref="DuplicateConcurrencyScopeForEventSourceId">Thrown when more than one scope has the same key.</exception>
    public EventsWithConcurrencyScopes(
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        Events = Array.AsReadOnly(events.ToArray());

        var materializedConcurrencyScopes = new Dictionary<EventSourceId, ConcurrencyScope>();
        foreach (var (eventSourceId, concurrencyScope) in concurrencyScopes)
        {
            if (!materializedConcurrencyScopes.TryAdd(eventSourceId, concurrencyScope))
            {
                throw new DuplicateConcurrencyScopeForEventSourceId(eventSourceId);
            }
        }

        ConcurrencyScopes = new ReadOnlyDictionary<EventSourceId, ConcurrencyScope>(materializedConcurrencyScopes);
    }

    /// <summary>
    /// Gets the materialized events in the order they will be submitted to the append operation.
    /// </summary>
    public IReadOnlyList<EventForEventSourceId> Events { get; }

    /// <summary>
    /// Gets the copied, source-keyed concurrency scopes.
    /// </summary>
    public IReadOnlyDictionary<EventSourceId, ConcurrencyScope> ConcurrencyScopes { get; }
}
