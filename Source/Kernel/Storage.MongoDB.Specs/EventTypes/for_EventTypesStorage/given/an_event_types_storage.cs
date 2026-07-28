// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using EventTypesStorage = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventTypesStorage;
using MongoEventType = Cratis.Chronicle.Storage.MongoDB.Events.EventTypes.EventType;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.given;

public class an_event_types_storage(MongoDBFixture fixture) : Specification
{
    protected static readonly EventTypeId _eventTypeId = "the-event-type";

    protected IEventStoreDatabase _database;
    protected EventTypesStorage _storage;
    protected IMongoCollection<BsonDocument> _storedDocuments;

    IMongoClient _client;
    string _databaseName;

    void Establish()
    {
        new ConventionPacks().Provide();

        var eventStore = new EventStoreName("test-event-store");
        _databaseName = $"chronicle_event_types_{Guid.NewGuid():N}";
        _client = new MongoClient(fixture.ConnectionString);
        var database = _client.GetDatabase(_databaseName);
        var collection = database.GetCollection<MongoEventType>(WellKnownCollectionNames.EventTypes);
        _storedDocuments = database.GetCollection<BsonDocument>(WellKnownCollectionNames.EventTypes);

        // The substitute doubles as a recording fake: the storage acquires the collection through it on
        // every read that reaches Mongo, so a cache hit shows up as no GetCollection call.
        _database = Substitute.For<IEventStoreDatabase>();
        _database.GetCollection<MongoEventType>(WellKnownCollectionNames.EventTypes).Returns(collection);

        _storage = new EventTypesStorage(eventStore, _database, Substitute.For<ILogger<EventTypesStorage>>());
    }

    protected async Task RegisterGeneration(EventTypeGeneration generation, string schemaJson)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        await _storage.Register(new EventType(_eventTypeId, generation), schema);
    }

    protected void ShouldNotHaveQueriedTheCollection() =>
        _database.DidNotReceive().GetCollection<MongoEventType>(WellKnownCollectionNames.EventTypes);

    protected void ShouldHaveQueriedTheCollection() =>
        _database.Received().GetCollection<MongoEventType>(WellKnownCollectionNames.EventTypes);

    protected void ClearRecordedCalls() => _database.ClearReceivedCalls();

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
