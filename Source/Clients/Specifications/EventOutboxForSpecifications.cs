// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Types;
using Cratis.Compliance;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Json;
using Cratis.Schemas;

namespace Cratis.Specifications;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for specifications.
/// </summary>
public class EventOutboxForSpecifications : IEventOutbox
{
    readonly EventSequenceForSpecifications _sequence;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventOutboxForSpecifications"/> class.
    /// </summary>
    public EventOutboxForSpecifications()
    {
        var schemaGenerator = new JsonSchemaGenerator(
            new ComplianceMetadataResolver(
                new KnownInstancesOf<ICanProvideComplianceMetadataForType>(),
                new KnownInstancesOf<ICanProvideComplianceMetadataForProperty>()));

        var typeFormats = new TypeFormats();
        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);
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
    public Task Append(EventSourceId eventSourceId, object @event, DateTimeOffset? validFrom = default) => _sequence.Append(eventSourceId, @event);

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
