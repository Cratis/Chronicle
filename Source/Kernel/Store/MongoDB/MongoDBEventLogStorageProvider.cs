// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events.Schemas;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLogStorageProvider"/> for MongoDB.
    /// </summary>
    public class MongoDBEventLogStorageProvider : IEventLogStorageProvider
    {
        readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabase;
        readonly ISchemaStore _schemaStore;
        readonly IJsonComplianceManager _jsonComplianceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBEventLogStorageProvider"/> class.
        /// </summary>
        /// <param name="eventStoreDatabase"><see cref="ProviderFor{T}"/> for <see cref="IEventStoreDatabase"/> to use.</param>
        /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
        /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
        public MongoDBEventLogStorageProvider(
            ProviderFor<IEventStoreDatabase> eventStoreDatabase,
            ISchemaStore schemaStore,
            IJsonComplianceManager jsonComplianceManager)
        {
            _eventStoreDatabase = eventStoreDatabase;
            _schemaStore = schemaStore;
            _jsonComplianceManager = jsonComplianceManager;
        }

        /// <inheritdoc/>
        public async Task<IEventCursor> GetFromSequenceNumber(
            EventLogSequenceNumber sequenceNumber,
            EventSourceId? eventSourceId = null,
            IEnumerable<EventType>? eventTypes = null)
        {
            var collection = _eventStoreDatabase().GetEventLogCollectionFor(EventLogId.Default);
            var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber)
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
            var cursor = await collection.FindAsync(filter);
            return new EventCursor(_schemaStore, _jsonComplianceManager, cursor);
        }
    }
}
