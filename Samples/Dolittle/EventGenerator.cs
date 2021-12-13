// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Concepts;
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
        static readonly Random _random = new();
        static readonly string[] _accountGuids = new[]
        {
                "626fb1ab-e74d-4bd9-a519-0e4268499fd6",
                "37e3c5b9-206c-435c-98f1-8971895c5059",
                "43e659e9-3d45-4600-a6db-c4d5b9f0d2b0",
                "200e206f-2796-4e96-952a-5fe64c49f430"
        };
        static readonly string[] _accountNames = new[]
        {
                "First Account",
                "Second Account",
                "Third Account",
                "Forth Account"
        };
        static readonly Guid[] _accountOwner = new[]
        {
                Guid.Parse("b6058430-0b22-4229-a819-a652a27c6a4a"),
                Guid.Parse("b6058430-0b22-4229-a819-a652a27c6a4a"),
                Guid.Parse("27d1d717-93aa-4aa2-ba83-fa6b7f817ab5"),
                Guid.Parse("27d1d717-93aa-4aa2-ba83-fa6b7f817ab5")
        };

        readonly IMongoDBClientFactory _mongoDBClientFactory;
        readonly IMongoCollection<Event> _collection;

        readonly Func<int, object>[] _eventTypeCreators = new Func<int, object>[]
        {
                (account) => new DebitAccountOpened(_accountNames[account], _accountOwner[account]),
                (_) => new DepositToDebitAccountPerformed(42),
                (_) => new WithdrawalFromDebitAccountPerformed(41)
        };

        public EventGenerator(IMongoDBClientFactory mongoDBClientFactory)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
            var client = _mongoDBClientFactory.Create(new MongoUrl("mongodb://localhost:27017"));
            var database = client.GetDatabase("event_store");
            _collection = database.GetCollection<Event>("event-log");
        }

        [HttpGet("debitaccountopened")]
        public async Task GenerateDebitAccountOpened()
        {
            var count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
            await InsertNewEvent((uint)count + 1, DateTimeOffset.UtcNow, 0);
        }

        [HttpGet("single")]
        public async Task GenerateSingle()
        {
            var count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
            await InsertNewEvent((uint)count + 1, DateTimeOffset.UtcNow);
        }

        [HttpGet]
        public async Task Generate()
        {
            var count = await _collection.CountDocumentsAsync(FilterDefinition<Event>.Empty);
            var occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, 0, new GregorianCalendar(), TimeSpan.Zero);

            for (var i = 0; i < 1000; i++)
            {
                occurred = occurred.Add(TimeSpan.FromMinutes(_random.Next() % 240));
                await InsertNewEvent((uint)count + 1 + (uint)i, occurred);
            }
        }

        async Task InsertNewEvent(uint? sequenceNumber = default, DateTimeOffset? occurred = default, int? eventTypeToCreate = default, int? account = default)
        {
            sequenceNumber ??= (uint)_collection.CountDocuments(FilterDefinition<Event>.Empty) + 1;
            occurred ??= DateTimeOffset.UtcNow;
            account ??= _random.Next() % _accountGuids.Length;
            var content = _eventTypeCreators[eventTypeToCreate ?? _random.Next() % _eventTypeCreators.Length](account.Value);
            var eventType = content.GetType().GetCustomAttribute<EventTypeAttribute>()!;
            var contentAsJson = JsonConvert.SerializeObject(content, new JsonConverter[] {
                    new ConceptAsJsonConverter(),
                    new ConceptAsDictionaryJsonConverter()
                });
            var contentAsBson = BsonDocument.Parse(contentAsJson);

            var @event = new Event(sequenceNumber.Value,
                new ExecutionContext(
                    Guid.NewGuid(),
                    Guid.Parse("bbbca70b-9eb0-4262-af27-b24c6eaffeef"),
                    Guid.Parse("445f8ea8-1a6f-40d7-b2fc-796dba92dc44"),
                    new Version(1, 0, 0, 0, ""),
                    "Development",
                    Array.Empty<Claim>()),
                new EventMetadata(
                    occurred.Value,
                    _accountGuids[account.Value],
                    eventType.Identifier,
                    eventType.Generation,
                    false),
                new Aggregate(false, Guid.Empty, 0, 0),
                new EventHorizon(false, 0, DateTimeOffset.MinValue, Guid.Empty),
                contentAsBson);

            await _collection.InsertOneAsync(@event);
        }
    }
}
