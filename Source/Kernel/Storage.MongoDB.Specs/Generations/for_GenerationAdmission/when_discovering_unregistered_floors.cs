// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationAdmission;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_discovering_unregistered_floors(ReplicaSetMongoDBFixture fixture) : given.a_provider(fixture)
{
    IReadOnlyDictionary<EventStoreName, GenerationFloor> _floors;
    string[] _before;
    string _foreignDatabase;

    async Task Establish()
    {
        await Advance(2);
        await _registry.DeleteManyAsync(FilterDefinition<EventStore>.Empty);
        var collection = _database.GetEventStoreDatabase(_name).GetCollection<BsonDocument>("generation-floor");
        var floor = await collection.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        _foreignDatabase = DatabaseNames.ForEventStore(_name, "x" + _prefix);
        string[] unrelated =
        [
            DatabaseNames.ForEventStoreNamespace(_name, new("a+es"), _prefix),
            DatabaseNames.ForReadModels(_name, new("a+es"), _prefix),
            DatabaseNames.ForReadModels(_name, EventStoreNamespaceName.Default, _prefix),
            DatabaseNames.WithPrefix(WellKnownDatabaseNames.Chronicle, _prefix),
            _foreignDatabase
        ];
        foreach (var name in unrelated)
        {
            await _client.GetDatabase(name).GetCollection<BsonDocument>("generation-floor").InsertOneAsync(floor.DeepClone().AsBsonDocument);
        }
        _before = await Databases();
    }

    async Task Because() => _floors = await _storage.ReadGenerationFloors();

    [Fact] void should_find_only_the_event_store_floor() => _floors.Keys.ShouldEqual([_name]);
    [Fact] void should_preserve_the_floor() => _floors[_name].Revision.ShouldEqual(1UL);
    [Fact] async Task should_leave_the_registry_empty() => (await _registry.CountDocumentsAsync(FilterDefinition<EventStore>.Empty)).ShouldEqual(0L);
    [Fact] async Task should_not_create_a_database() => (await Databases()).ShouldEqual(_before);

    async Task Destroy() => await _client.DropDatabaseAsync(_foreignDatabase);
}
