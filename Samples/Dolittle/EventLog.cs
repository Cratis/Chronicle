// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Concepts;
using Cratis.Extensions.Dolittle.EventStore;
using Cratis.Extensions.MongoDB;
using Dolittle.SDK.Events;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using ExecutionContext = Cratis.Extensions.Dolittle.EventStore.ExecutionContext;
using Version = Cratis.Extensions.Dolittle.EventStore.Version;

namespace Sample
{
    public class EventLog : IEventLog
    {
        readonly IMongoDBClientFactory _mongoDBClientFactory;
        readonly IMongoCollection<Event> _collection;

        public EventLog(IMongoDBClientFactory mongoDBClientFactory)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
            var client = _mongoDBClientFactory.Create(new MongoUrl("mongodb://localhost:27017"));
            var database = client.GetDatabase("event_store");
            _collection = database.GetCollection<Event>("event-log");
        }

        public async Task Append(EventSourceId eventSourceId, object @event)
        {
            var sequenceNumber = (uint)_collection.CountDocuments(FilterDefinition<Event>.Empty) + 1;
            var eventType = @event.GetType().GetCustomAttribute<EventTypeAttribute>();
            var contentAsJson = JsonConvert.SerializeObject(@event, new JsonConverter[] {
                    new ConceptAsJsonConverter(),
                    new ConceptAsDictionaryJsonConverter()
                });
            var contentAsBson = BsonDocument.Parse(contentAsJson);

            var eventToInsert = new Event(sequenceNumber,
                new ExecutionContext(
                    Guid.NewGuid(),
                    Guid.Parse("bbbca70b-9eb0-4262-af27-b24c6eaffeef"),
                    Guid.Parse("445f8ea8-1a6f-40d7-b2fc-796dba92dc44"),
                    new Version(1, 0, 0, 0, ""),
                    "Development",
                    Array.Empty<Claim>()),
                new EventMetadata(
                    DateTimeOffset.UtcNow,
                    eventSourceId,
                    eventType!.Identifier,
                    eventType!.Generation,
                    false),
                new Aggregate(false, Guid.Empty, 0, 0),
                new EventHorizon(false, 0, DateTimeOffset.MinValue, Guid.Empty),
                contentAsBson);

            await _collection.InsertOneAsync(eventToInsert);
        }
    }
}
