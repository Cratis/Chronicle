// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Events.EventTypes;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventTypes.for_EventTypesStorage.given;

public class two_event_types_storages(MongoDBFixture fixture) : Specification
{
    protected IEventTypesStorage _storageA;
    protected IEventTypesStorage _storageB;
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
        var collection = database.GetCollection<EventType>(WellKnownCollectionNames.EventTypes);
        _storedDocuments = database.GetCollection<BsonDocument>(WellKnownCollectionNames.EventTypes);

        // Two independent storage instances, mirroring two silos that share one Mongo collection.
        var databaseForA = Substitute.For<IEventStoreDatabase>();
        databaseForA.GetCollection<EventType>(WellKnownCollectionNames.EventTypes).Returns(collection);
        var databaseForB = Substitute.For<IEventStoreDatabase>();
        databaseForB.GetCollection<EventType>(WellKnownCollectionNames.EventTypes).Returns(collection);

        _storageA = new EventTypesStorage(eventStore, databaseForA, Substitute.For<ILogger<EventTypesStorage>>());
        _storageB = new EventTypesStorage(eventStore, databaseForB, Substitute.For<ILogger<EventTypesStorage>>());
    }

    async Task Destroy() => await _client.DropDatabaseAsync(_databaseName);
}
