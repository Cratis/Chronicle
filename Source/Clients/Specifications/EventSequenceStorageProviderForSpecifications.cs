// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.Storage.EventSequences;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSequenceStorage"/>.
/// </summary>
public class EventSequenceStorageProviderForSpecifications : IEventSequenceStorage
{
    readonly EventLogForSpecifications _eventLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceStorageProviderForSpecifications"/> class.
    /// </summary>
    /// <param name="eventLog">The <see creF="EventLogForSpecifications"/>.</param>
    public EventSequenceStorageProviderForSpecifications(EventLogForSpecifications eventLog)
    {
        _eventLog = eventLog;
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = null, IEnumerable<EventType>? eventTypes = null, CancellationToken cancellationToken = default)
    {
        var query = _eventLog.AppendedEvents.Where(_ => _.Metadata.SequenceNumber >= sequenceNumber);
        if (eventSourceId is not null)
        {
            query = query.Where(_ => _.Context.EventSourceId == eventSourceId);
        }
        if (eventTypes is not null)
        {
            query = query.Where(_ => eventTypes.Any(et => et.Id == _.Metadata.Type.Id));
        }

        var cursor = new EventCursorForSpecifications(query.ToArray());
        return Task.FromResult<IEventCursor>(cursor);
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(EventSequenceId eventSequenceId, EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = null, IEnumerable<EventType>? eventTypes = null, CancellationToken cancellationToken = default)
    {
        var query = _eventLog.AppendedEvents.Where(_ => _.Metadata.SequenceNumber >= start && _.Metadata.SequenceNumber <= end);
        if (eventSourceId is not null)
        {
            query = query.Where(_ => _.Context.EventSourceId == eventSourceId);
        }
        if (eventTypes is not null)
        {
            query = query.Where(_ => eventTypes.Any(et => et.Id == _.Metadata.Type.Id));
        }

        var cursor = new EventCursorForSpecifications(query.ToArray());
        return Task.FromResult<IEventCursor>(cursor);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetHeadSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null) => Task.FromResult(_eventLog.AppendedEvents.First().Metadata.SequenceNumber);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null) => Task.FromResult(_eventLog.AppendedEvents.Last().Metadata.SequenceNumber);

    /// <inheritdoc/>
    public Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes) => Task.FromResult<IImmutableDictionary<EventType, EventSequenceNumber>>(ImmutableDictionary<EventType, EventSequenceNumber>.Empty);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null)
    {
        var query = _eventLog.AppendedEvents.Where(_ => _.Metadata.SequenceNumber >= sequenceNumber);
        if (eventSourceId is not null)
        {
            query = query.Where(_ => _.Context.EventSourceId == eventSourceId);
        }
        if (eventTypes is not null)
        {
            query = query.Where(_ => eventTypes.Any(et => et.Id == _.Metadata.Type.Id));
        }

        var nextSequenceNumber = query.First().Metadata.SequenceNumber;
        return Task.FromResult(nextSequenceNumber);
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> GetLastInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        var lastInstance = _eventLog.AppendedEvents.Where(_ => _.Metadata.Type.Id == eventTypeId && _.Context.EventSourceId == eventSourceId).OrderByDescending(_ => _.Metadata.SequenceNumber).First();
        return Task.FromResult(new AppendedEvent(lastInstance.Metadata, lastInstance.Context, lastInstance.Content));
    }

    /// <inheritdoc/>
    public Task<AppendedEvent> GetLastInstanceOfAny(EventSequenceId eventSequenceId, EventSourceId eventSourceId, IEnumerable<EventTypeId> eventTypes)
    {
        var lastInstance = _eventLog.AppendedEvents.Where(_ => eventTypes.Any(et => et == _.Metadata.Type.Id) && _.Context.EventSourceId == eventSourceId).OrderByDescending(_ => _.Metadata.SequenceNumber).First();
        return Task.FromResult(new AppendedEvent(lastInstance.Metadata, lastInstance.Context, lastInstance.Content));
    }

    /// <inheritdoc/>
    public Task<bool> HasInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        var count = _eventLog.AppendedEvents.Count(_ => _.Metadata.Type.Id == eventTypeId && _.Context.EventSourceId == eventSourceId);
        return Task.FromResult(count > 0);
    }

    /// <inheritdoc/>
    public Task Append(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId eventSourceId, EventType eventType, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred, DateTimeOffset validFrom, ExpandoObject content) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task Compensate(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventType eventType, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred, DateTimeOffset validFrom, ExpandoObject content) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<AppendedEvent> Redact(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, RedactionReason reason, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<IEnumerable<EventType>> Redact(EventSequenceId eventSequenceId, EventSourceId eventSourceId, RedactionReason reason, IEnumerable<EventType>? eventTypes, IEnumerable<Causation> causation, IEnumerable<IdentityId> causedByChain, DateTimeOffset occurred) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<AppendedEvent> GetEventAt(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<EventCount> GetCount(EventSequenceId eventSequenceId, EventSequenceNumber? lastEventSequenceNumber = null, IEnumerable<EventType>? eventTypes = null) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes) => throw new NotImplementedException();
}
