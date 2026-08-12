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
/// Both inputs, including each scope's nested <see cref="ConcurrencyScope.EventTypes"/>, are materialized when this
/// value is created. For a label that targets an event source in
/// <see cref="Events"/>, a missing scope or <see cref="ConcurrencyScope.NotSet"/> retains the normal
/// <see cref="IEventSequence.AppendMany(IEnumerable{EventForEventSourceId}, CorrelationId?, IEnumerable{string}?, IDictionary{EventSourceId, ConcurrencyScope}?)"/>
/// strategy resolution. An independent non-target label must use a concrete exact scope or
/// <see cref="ConcurrencyScope.None"/>. A scope label does not need to match an event target unless the scope narrows
/// by <see cref="ConcurrencyScope.EventSourceId"/>; when it does, the label and narrowed event source must match.
/// </remarks>
public sealed class EventsWithConcurrencyScopes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventsWithConcurrencyScopes"/> class.
    /// </summary>
    /// <param name="events">The events to append, in append order.</param>
    /// <param name="concurrencyScopes">The independently labeled concurrency scopes to pass to the append operation.</param>
    /// <exception cref="ConcurrencyScopeLabelMustBeSpecified">Thrown when an event target or scope label is unspecified, blank, or whitespace.</exception>
    /// <exception cref="DuplicateConcurrencyScopeForEventSourceId">Thrown when more than one scope has the same key.</exception>
    /// <exception cref="ConcurrencyScopeEventSourceIdDoesNotMatchLabel">Thrown when a scope narrows by an event source different from its label.</exception>
    /// <exception cref="IndependentConcurrencyScopeMustBeExplicit">Thrown when an independent non-target label has a scope that cannot be validated explicitly.</exception>
    public EventsWithConcurrencyScopes(
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        var materializedEvents = events.ToArray();
        foreach (var @event in materializedEvents)
        {
            ThrowIfLabelIsNotSpecified(@event.EventSourceId);
        }

        Events = Array.AsReadOnly(materializedEvents);
        var eventTargets = materializedEvents.Select(_ => _.EventSourceId).ToHashSet();

        var materializedConcurrencyScopes = new Dictionary<EventSourceId, ConcurrencyScope>();
        foreach (var (eventSourceId, concurrencyScope) in concurrencyScopes)
        {
            ThrowIfLabelIsNotSpecified(eventSourceId);

            var materializedConcurrencyScope = Materialize(concurrencyScope);
            if (materializedConcurrencyScope.EventSourceId is not null && materializedConcurrencyScope.EventSourceId != eventSourceId)
            {
                throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(eventSourceId, materializedConcurrencyScope.EventSourceId);
            }

            if (!eventTargets.Contains(eventSourceId) &&
                (materializedConcurrencyScope == ConcurrencyScope.NotSet || materializedConcurrencyScope.IsIncomplete))
            {
                throw new IndependentConcurrencyScopeMustBeExplicit(eventSourceId);
            }

            if (!materializedConcurrencyScopes.TryAdd(eventSourceId, materializedConcurrencyScope))
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
    /// Gets the copied, independently labeled concurrency scopes.
    /// </summary>
    public IReadOnlyDictionary<EventSourceId, ConcurrencyScope> ConcurrencyScopes { get; }

    static ConcurrencyScope Materialize(ConcurrencyScope concurrencyScope) =>
        concurrencyScope.EventTypes is null
            ? concurrencyScope
            : concurrencyScope with { EventTypes = concurrencyScope.EventTypes.ToArray() };

    static void ThrowIfLabelIsNotSpecified(EventSourceId label)
    {
        if (label == EventSourceId.Unspecified || string.IsNullOrWhiteSpace(label.Value))
        {
            throw new ConcurrencyScopeLabelMustBeSpecified();
        }
    }
}
