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
            var accountGuids = new Guid[]
            {
                Guid.Parse("626fb1ab-e74d-4bd9-a519-0e4268499fd6"),
                Guid.Parse("37e3c5b9-206c-435c-98f1-8971895c5059"),
                Guid.Parse("43e659e9-3d45-4600-a6db-c4d5b9f0d2b0"),
                Guid.Parse("200e206f-2796-4e96-952a-5fe64c49f430")
            };
            var accountNames = new string[]
            {
                "First Account",
                "Second Account",
                "Third Account",
                "Forth Account"
            };
            var accountOwner = new Guid[]
            {
                Guid.Parse("b6058430-0b22-4229-a819-a652a27c6a4a"),
                Guid.Parse("b6058430-0b22-4229-a819-a652a27c6a4a"),
                Guid.Parse("27d1d717-93aa-4aa2-ba83-fa6b7f817ab5"),
                Guid.Parse("27d1d717-93aa-4aa2-ba83-fa6b7f817ab5")
            };

            var client = _mongoDBClientFactory.Create(new MongoUrl("mongodb://localhost:27017"));
            var database = client.GetDatabase("event_store");
            var collection = database.GetCollection<Event>("event-log");
            var occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, new GregorianCalendar(), TimeSpan.Zero);
            var random = new Random();
            var eventTypeCreators = new Func<int, object>[]
            {
                (account) => new DebitAccountOpened(accountNames[account], accountOwner[account] , Guid.Empty),
                (_) => new DepositToDebitAccountPerformed(42),
                (_) => new WithdrawalFromDebitAccountPerformed(41)
            };

            for (var i = 0; i < 1000; i++)
            {
                var account = random.Next() % accountGuids.Length;
                var content = eventTypeCreators[random.Next() % eventTypeCreators.Length](account);
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
                        accountGuids[account],
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
