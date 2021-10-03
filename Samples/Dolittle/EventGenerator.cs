// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Extensions.Dolittle.EventStore;
using Cratis.Extensions.MongoDB;
using Dolittle.SDK.Events;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using ExecutionContext = Cratis.Extensions.Dolittle.EventStore.ExecutionContext;
using Version = Cratis.Extensions.Dolittle.EventStore.Version;

namespace Sample
{
    [Route("/api/events/generate")]
    public class EventGenerator : Controller
    {
        readonly IMongoDBClientFactory _mongoDBClientFactory;

        public EventGenerator(IMongoDBClientFactory mongoDBClientFactory)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
        }

        [HttpGet]
        public async Task Generate()
        {
            var client = _mongoDBClientFactory.Create(new MongoUrl("mongodb://localhost:27017"));
            var database = client.GetDatabase("event_store");
            var collection = database.GetCollection<Event>("event-log");
            var occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, new GregorianCalendar(), TimeSpan.Zero);
            var random = new Random();
            var eventTypeCreators = new Func<object>[]
            {
                () => new DebitAccountOpened("Blah", Guid.NewGuid(), Guid.NewGuid()),
                () => new DepositToDebitAccountPerformed(42),
                () => new WithdrawalFromDebitAccountPerformed(41)
            };

            for (var i = 0; i < 1000; i++)
            {
                var content = eventTypeCreators[random.Next() % eventTypeCreators.Length]();
                var eventType = content.GetType().GetCustomAttribute<EventTypeAttribute>()!;
                var contentAsJson = JsonConvert.SerializeObject(content);
                var contentAsBson = BsonDocument.Parse(contentAsJson);

                occurred = occurred.Add(TimeSpan.FromMinutes(random.Next()%240));
                var @event = new Event((uint)i,
                    new ExecutionContext(
                        Guid.NewGuid(),
                        Guid.Parse("bbbca70b-9eb0-4262-af27-b24c6eaffeef"),
                        Guid.Parse("445f8ea8-1a6f-40d7-b2fc-796dba92dc44"),
                        new Version(1, 0, 0, 0, ""),
                        "Development",
                        Array.Empty<Claim>()),
                    new EventMetadata(
                        occurred,
                        Guid.NewGuid(),
                        eventType.EventType.Id,
                        eventType.EventType.Generation,
                        false),
                    new Aggregate(false, Guid.Empty, 0, 0),
                    new EventHorizon(false, 0, DateTimeOffset.MinValue, Guid.Empty),
                    contentAsBson);

                await collection.InsertOneAsync(@event);

                Console.Write(".");
            }
        }
    }
}
