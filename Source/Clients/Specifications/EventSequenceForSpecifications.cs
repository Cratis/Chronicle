// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Json;

namespace Cratis.Chronicle.Specifications;

/// <summary>
/// Represents an event sequence for working in-memory.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceForSpecifications"/> class.
/// </remarks>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/>.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/>.</param>
public class EventSequenceForSpecifications(
    IExpandoObjectConverter expandoObjectConverter,
    IJsonSchemaGenerator schemaGenerator)
{
    readonly List<AppendedEventForSpecifications> _appendedEvents = [];
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
    /// <param name="validFrom">Optional date and time for when the compensation is valid from. </param>
    /// <returns>Awaitable task.</returns>
    public Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default)
    {
        var serialized = JsonSerializer.Serialize(@event, Globals.JsonSerializerOptions)!;
        var schema = schemaGenerator.Generate(@event.GetType());
        var eventAsExpando = expandoObjectConverter.ToExpandoObject((JsonNode.Parse(serialized) as JsonObject)!, schema);
        var eventTypeAttribute = @event.GetType().GetCustomAttribute<EventTypeAttribute>();
        _appendedEvents.Add(new(
            new(_sequenceNumber, eventTypeAttribute!.Type),
            new(
                eventSourceId,
                _sequenceNumber,
                DateTimeOffset.UtcNow,
                validFrom ?? DateTimeOffset.MinValue,
                EventStoreName.NotSet,
                EventStoreNamespaceName.Default,
                CorrelationId.New(),
                [],
                Identity.System),
            eventAsExpando,
            @event));
        _sequenceNumber++;

        return Task.CompletedTask;
    }
}
