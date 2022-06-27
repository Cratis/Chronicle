// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Specifications;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSequenceStorageProvider"/>.
/// </summary>
public class EventSequenceStorageProviderForSpecifications : IEventSequenceStorageProvider
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
    public Task<IEventCursor> GetFromSequenceNumber(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber, EventSourceId? eventSourceId = null, IEnumerable<EventType>? eventTypes = null)
    {
        var query = _eventLog.AppendedEvents.Where(_ => _.Metadata.SequenceNumber >= sequenceNumber);
        if (eventSourceId is not null)
        {
            query = query.Where(_ => _.Context.EventSourceId == eventSourceId);
        }
        if (eventTypes is not null)
        {
            query = query.Where(_ => eventTypes.Any(et => et == _.Metadata.Type));
        }

        var cursor = new EventCursorForSpecifications(query.ToArray());
        return Task.FromResult<IEventCursor>(cursor);
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetRange(EventSequenceId eventSequenceId, EventSequenceNumber start, EventSequenceNumber end, EventSourceId? eventSourceId = null, IEnumerable<EventType>? eventTypes = null)
    {
        var query = _eventLog.AppendedEvents.Where(_ => _.Metadata.SequenceNumber >= start && _.Metadata.SequenceNumber <= end);
        if (eventSourceId is not null)
        {
            query = query.Where(_ => _.Context.EventSourceId == eventSourceId);
        }
        if (eventTypes is not null)
        {
            query = query.Where(_ => eventTypes.Any(et => et == _.Metadata.Type));
        }

        var cursor = new EventCursorForSpecifications(query.ToArray());
        return Task.FromResult<IEventCursor>(cursor);
    }

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetHeadSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null) => Task.FromResult(_eventLog.AppendedEvents.First().Metadata.SequenceNumber);

    /// <inheritdoc/>
    public Task<EventSequenceNumber> GetTailSequenceNumber(EventSequenceId eventSequenceId, IEnumerable<EventType>? eventTypes = null, EventSourceId? eventSourceId = null) => Task.FromResult(_eventLog.AppendedEvents.Last().Metadata.SequenceNumber);

    /// <inheritdoc/>
    public Task<AppendedEvent> GetLastInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        var lastInstance = _eventLog.AppendedEvents.Where(_ => _.Metadata.Type.Id == eventTypeId && _.Context.EventSourceId == eventSourceId).OrderByDescending(_ => _.Metadata.SequenceNumber).First();
        return Task.FromResult(new AppendedEvent(lastInstance.Metadata, lastInstance.Context, lastInstance.Content));
    }
}
