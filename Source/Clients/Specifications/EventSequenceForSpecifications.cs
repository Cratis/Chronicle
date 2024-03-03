// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Execution;
using Aksio.Json;
using Cratis.Auditing;
using Cratis.Events;
using Cratis.Identities;
using Cratis.Json;
using Cratis.Schemas;

namespace Cratis.Specifications;

/// <summary>
/// Represents an event sequence for working in-memory.
/// </summary>
public class EventSequenceForSpecifications
{
    readonly List<AppendedEventForSpecifications> _appendedEvents = new();
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly IJsonSchemaGenerator _schemaGenerator;
    EventSequenceNumber _sequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceForSpecifications"/> class.
    /// </summary>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/>.</param>
    public EventSequenceForSpecifications(
        IExpandoObjectConverter expandoObjectConverter,
        IJsonSchemaGenerator schemaGenerator)
    {
        _expandoObjectConverter = expandoObjectConverter;
        _schemaGenerator = schemaGenerator;
    }

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
        var schema = _schemaGenerator.Generate(@event.GetType());
        var eventAsExpando = _expandoObjectConverter.ToExpandoObject((JsonNode.Parse(serialized) as JsonObject)!, schema);
        var eventTypeAttribute = @event.GetType().GetCustomAttribute<EventTypeAttribute>();
        _appendedEvents.Add(new(
            new(_sequenceNumber, eventTypeAttribute!.Type),
            new(
                eventSourceId,
                _sequenceNumber,
                DateTimeOffset.UtcNow,
                validFrom ?? DateTimeOffset.MinValue,
                TenantId.Development,
                CorrelationId.New(),
                ImmutableList<Causation>.Empty,
                Identity.System),
            eventAsExpando,
            @event));
        _sequenceNumber++;

        return Task.CompletedTask;
    }
}
