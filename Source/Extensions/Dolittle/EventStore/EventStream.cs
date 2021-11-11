// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.SDK.Events;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStream"/>.
    /// </summary>
    public class EventStream : IEventStream
    {
        readonly IMongoCollection<Event> _collection;
        readonly ILogger<EventStream> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStream"/> class.
        /// </summary>
        /// <param name="collection"><see cref="IMongoCollection{T}"/> that holds the event stream.</param>
        /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
        public EventStream(IMongoCollection<Event> collection, ILogger<EventStream> logger)
        {
            _collection = collection;
            _logger = logger;
        }

        /// <inheritdoc/>
        public IChangeStreamCursor<ChangeStreamDocument<Event>> Watch() => _collection.Watch(options: new() { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup });

        /// <inheritdoc/>
        public long Count() => _collection.CountDocuments(FilterDefinition<Event>.Empty);

        /// <inheritdoc/>
        public async Task<IAsyncCursor<Event>> GetFromPosition(uint position, IEnumerable<EventType>? eventTypes = default, EventSourceId? eventSourceId = default)
        {
            var offsetFilter = Builders<Event>.Filter.Gt(_ => _.Id, position);
            var eventTypeFilters = eventTypes?.Select(_ => Builders<Event>.Filter.Eq(_ => _.Metadata.TypeId, _.Id.Value)).ToArray() ?? Array.Empty<FilterDefinition<Event>>();
            var eventSourceFilter = (eventSourceId is null) ? FilterDefinition<Event>.Empty : Builders<Event>.Filter.Eq(_ => _.Metadata.EventSource, eventSourceId.Value);

            var filter = Builders<Event>.Filter.And(
                offsetFilter,
                eventSourceFilter,
                Builders<Event>.Filter.Or(eventTypeFilters)
            );

            _logger.GettingEventsFromOffset(position);

            return await _collection.FindAsync(
                filter,
                new()
                {
                    Sort = Builders<Event>.Sort.Ascending(_ => _.Id)
                });
        }
    }
}
