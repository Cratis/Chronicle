// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Json;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an event sequence for working in-memory.
/// </summary>
public class EventSequenceForSpecifications
{
    readonly List<AppendedEventForSpecifications> _appendedEvents = new();
    EventSequenceNumber _sequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _appendedEvents;

    /// <summary>
    /// Append event to sequence.
    /// </summary>
    /// <param name="eventSourceId">The event source to append for.</param>
    /// <param name="event">Event to append.</param>
    /// <returns>Awaitable task.</returns>
    public Task Append(EventSourceId eventSourceId, object @event)
    {
        var eventTypeAttribute = @event.GetType().GetCustomAttribute<EventTypeAttribute>();
        _appendedEvents.Add(new(
            new(_sequenceNumber, eventTypeAttribute!.Type),
            new(
                eventSourceId,
                _sequenceNumber,
                DateTimeOffset.UtcNow,
                DateTimeOffset.MinValue,
                TenantId.Development,
                CorrelationId.New(),
                CausationId.System,
                CausedBy.System),
            @event.AsExpandoObject(true),
            @event));
        _sequenceNumber++;

        return Task.CompletedTask;
    }
}
