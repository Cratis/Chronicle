// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for specifications.
/// </summary>
public class EventLogForSpecifications : IEventLog
{
    readonly EventSequenceForSpecifications _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventLogForSpecifications"/> class.
    /// </summary>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/>.</param>
    public EventLogForSpecifications(
          IExpandoObjectConverter expandoObjectConverter,
          IJsonSchemaGenerator schemaGenerator)
    {
        _sequence = new(expandoObjectConverter, schemaGenerator);
    }

    /// <summary>
    /// Gets the appended events.
    /// </summary>
    public IEnumerable<AppendedEventForSpecifications> AppendedEvents => _sequence.AppendedEvents;

    /// <inheritdoc/>
    public Task<IImmutableList<AppendedEvent>> GetForEventSourceIdAndEventTypes(EventSourceId eventSourceId, IEnumerable<EventType> eventTypes) =>
        Task.FromResult<IImmutableList<AppendedEvent>>(ImmutableList<AppendedEvent>.Empty);

    /// <inheritdoc/>
    public Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = null) => _sequence.Append(eventSourceId, @event, validFrom);

    /// <inheritdoc/>
    public async Task AppendMany(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            await _sequence.Append(eventSourceId, @event);
        }
    }

    /// <inheritdoc/>
    public async Task AppendMany(EventSourceId eventSourceId, IEnumerable<EventAndValidFrom> events)
    {
        foreach (var @event in events)
        {
            await _sequence.Append(eventSourceId, @event.Event, @event.ValidFrom);
        }
    }

    /// <inheritdoc/>
    public Task Redact(EventSequenceNumber sequenceNumber, RedactionReason? reason = null) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Redact(EventSourceId eventSourceId, RedactionReason? reason = null, params Type[] eventTypes) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumber() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumberForObserver(Type type) => throw new NotImplementedException();
}
