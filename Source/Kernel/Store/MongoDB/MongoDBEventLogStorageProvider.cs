// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventLogStorageProvider"/> for MongoDB.
/// </summary>
public class MongoDBEventLogStorageProvider : IEventLogStorageProvider
{
    readonly IEventConverter _converter;
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBEventLogStorageProvider"/> class.
    /// </summary>
    /// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/> to use.</param>
    public MongoDBEventLogStorageProvider(
        IEventConverter converter,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider)
    {
        _converter = converter;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = _eventStoreDatabaseProvider().GetEventSequenceCollectionFor(EventSequenceId.Log);
        var @event = await collection.Find(filter).SortByDescending(_ => _.SequenceNumber).Limit(1).SingleAsync();
        return await _converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = null,
        IEnumerable<EventType>? eventTypes = null)
    {
        var collection = _eventStoreDatabaseProvider().GetEventSequenceCollectionFor(EventSequenceId.Log);
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber.Value)
            };

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        if (eventTypes?.Any() == true)
        {
            filters.Add(Builders<Event>.Filter.Or(eventTypes.Select(_ => Builders<Event>.Filter.Eq(e => e.Type, _.Id)).ToArray()));
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());
#pragma warning disable CA1849, MA0042 // Allow this blocking call - we get deadlocks when using FindAsync() / ToCursorAsync()
        var cursor = collection.Find(filter).ToCursor();
        return Task.FromResult<IEventCursor>(new EventCursor(_converter, cursor));
    }
}
