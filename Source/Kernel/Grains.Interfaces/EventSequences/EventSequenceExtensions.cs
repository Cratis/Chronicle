// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Json;
using Cratis.Monads;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Extension methods for the <see cref="IEventSequence"/> interface.
/// </summary>
public static class EventSequenceExtensions
{
    /// <summary>
    /// Appends an event to the event sequence.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> to append the event to.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> of the event source.</param>
    /// <param name="event">The event to append.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> for the event.</param>
    /// <param name="causation">Collection of <see cref="Causation"/> for the event.</param>
    /// <param name="causedBy">The <see cref="Identity"/> of the entity that caused the event.</param>
    /// <param name="eventSourceType">Optional <see cref="EventSourceType"/> to specify the type of the event source. Defaults to <see cref="EventSourceType.Default"/>.</param>
    /// <param name="eventStreamType">Optional <see cref="EventStreamType"/> to specify the type of the event stream. Defaults to <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">Optional <see cref="EventStreamId"/> to specify the identifier of the event stream. Defaults to <see cref="EventStreamId.Default"/>.</param>
    /// <returns>An <see cref="AppendResult"/> indicating the result of the append operation.</returns>
    public static async Task<AppendResult> Append(
        this IEventSequence eventSequence,
        EventSourceId eventSourceId,
        object @event,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        Identity causedBy,
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default)
    {
        var eventType = @event.GetType().GetEventType();
        var content = (JsonSerializer.SerializeToNode(@event, Globals.JsonSerializerOptions) as JsonObject)!;
        return await eventSequence.Append(
            eventSourceType ?? EventSourceType.Default,
            eventSourceId,
            eventStreamType ?? EventStreamType.All,
            eventStreamId ?? EventStreamId.Default,
            eventType,
            content,
            correlationId,
            causation,
            causedBy,
            ConcurrencyScope.None);
    }
}
