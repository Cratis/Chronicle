// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Events.Schemas;
using Cratis.Extensions.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoEvent = Cratis.Extensions.Dolittle.EventStore.Event;

namespace Cratis.Extensions.Dolittle.EventStore.Api
{
    [Route("/api/events/store/log")]
    public class EventLog : Controller
    {
        readonly IMongoDatabase _database;
        readonly ISchemaStore _schemaStore;

        public EventLog(IMongoDBClientFactory mongoDBClientFactory, ISchemaStore schemaStore)
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
            var client = mongoDBClientFactory.Create(settings);
            _database = client.GetDatabase("event_store");
            _schemaStore = schemaStore;
        }

        [HttpGet("{eventLogId}")]
        public async Task<IEnumerable<Event>> FindFor(
            [FromRoute] string eventLogId)
        {
            var collection = GetMongoCollection();
            var findOptions = new FindOptions<MongoEvent>
            {
                Limit = 20
            };
            var result = await collection.FindAsync(_ => true, findOptions);
            var events = result.ToList();

            return events.Select(async _ =>
            {
                var name = "[N/A]";
                if (await _schemaStore.HasFor(_.Metadata.TypeId, _.Metadata.TypeGeneration))
                {
                    var schema = await _schemaStore.GetFor(_.Metadata.TypeId, _.Metadata.TypeGeneration);
                    name = schema.Schema.GetDisplayName();
                }
                return new Event(_.Id, name, _.Metadata.Occurred, JsonDocument.Parse(_.Content.ToJson()));
            }).Select(_ => _.Result).ToArray();
        }

        [HttpGet("{eventLogId}/count")]
        public Task<long> Count() => GetMongoCollection().CountDocumentsAsync(FilterDefinition<MongoEvent>.Empty);

        [HttpGet("histogram")]
        public Task<IEnumerable<EventHistogramEntry>> Histogram([FromRoute] string eventLogId)
        {
            var collection = GetMongoCollection();

            var group = new BsonDocument("$group",
                    new BsonDocument
                        {
                            { "_id",
                                new BsonDocument("$add",
                                new BsonArray
                                {
                                    new BsonDocument("$dayOfYear", "$Metadata.Occurred"),
                                    new BsonDocument("$multiply",
                                    new BsonArray
                                        {
                                            400,
                                            new BsonDocument("$year", "$Metadata.Occurred")
                                        })
                                }) },
                            { "count",
                                new BsonDocument("$sum", 1) },
                                { "first",
                                    new BsonDocument("$min", "$Metadata.Occurred") }
                        });
            var sort = new BsonDocument("$sort",
                    new BsonDocument("_id", 1));
            var project = new BsonDocument("$project",
                    new BsonDocument
                        {
                            { "date", "$first" },
                            { "count", 1 },
                            { "_id", 0 }
                        });

            var pipeline = new[] { group, sort, project };
            var result = collection.Aggregate<EventHistogramEntry>(pipeline).ToList();
            return Task.FromResult(result as IEnumerable<EventHistogramEntry>);
        }

        [HttpGet("{eventLogId}/types")]
        public Task Types([FromRoute] string eventLogId)
        {
            return Task.CompletedTask;
        }

        IMongoCollection<MongoEvent> GetMongoCollection()
        {
            return _database.GetCollection<MongoEvent>("event-log");
        }
    }
}
