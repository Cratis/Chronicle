// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.EventStore;
using Cratis.Extensions.MongoDB;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Extensions.Dolittle
{
    [Route("/api/events/store/log/{eventLogId}")]
    public class EventLog : Controller
    {
        readonly IMongoDatabase _database;

        public EventLog(IMongoDBClientFactory mongoDBClientFactory)
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
            var client = mongoDBClientFactory.Create(settings);
            _database = client.GetDatabase("event_store");
        }

        [HttpGet]
        public Task FindFor(
            [FromRoute] string eventLogId,
            [FromBody] EventFilter? filter)
        {
            return Task.CompletedTask;
        }

        [HttpGet("histogram")]
        public Task<IEnumerable<EventHistogramEntry>> Histogram([FromRoute] string eventLogId)
        {
            var collection = _database.GetCollection<Event>("event-log");

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

        [HttpGet("types")]
        public Task Types([FromRoute] string eventLogId)
        {
            return Task.CompletedTask;
        }
    }
}
