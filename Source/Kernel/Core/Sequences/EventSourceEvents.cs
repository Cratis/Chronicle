// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents whether an event source has any events in an event sequence.
/// </summary>
/// <param name="HasEvents">Whether the event source has any events.</param>
/// <remarks>
/// The answer is a single value computed on demand from storage rather than a projected read model, but it is
/// still declared as one: a query has to return its own read model for Arc to give it a route, and wrapping the
/// value also leaves room to say more about the event source later without breaking the shape.
/// </remarks>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record EventSourceEvents(bool HasEvents)
{
    /// <summary>
    /// Checks whether an event source has any events in an event sequence.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read from.</param>
    /// <param name="eventStore">Event store to check in.</param>
    /// <param name="namespace">Namespace to check in.</param>
    /// <param name="eventSequenceId">Event sequence to check in.</param>
    /// <param name="eventSourceId">The event source to check for.</param>
    /// <returns>Whether the event source has any events.</returns>
    public static async Task<EventSourceEvents> HasEventsForEventSourceId(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId) =>
        new(await storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId).HasEventsFor(eventSourceId));
}
