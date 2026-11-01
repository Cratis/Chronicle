// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Observation.Registrations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Registrations.for_ObserverRegistrationsStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_persisting_registrations(ReplicaSetMongoDBFixture fixture) : ObserverRegistrationsStorageContract
{
    MongoClient _client;
    IMongoDatabase _database;
    string _name;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _name = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_name);
    }

    async Task Destroy()
    {
        await _client.DropDatabaseAsync(_name);
        await _client.DropDatabaseAsync($"{_name}_other");
    }

    [Fact] public async Task should_create_a_unique_observer_and_sequence_index()
    {
        await Open().TrySave(Registration(), new(0));
        using var cursor = await _database.GetCollection<BsonDocument>(WellKnownCollectionNames.ObserverRegistrations).Indexes.ListAsync();
        var index = (await cursor.ToListAsync()).Single(item => item["name"] == "observer-sequence");
        index["unique"].AsBoolean.ShouldBeTrue();
        index["key"].AsBsonDocument.Names.ShouldEqual(["observerId", "eventSequenceId"]);
    }

    protected override IObserverRegistrationsStorage Open() => new ObserverRegistrationsStorage(Store(_database), SerializerOptions);

    protected override Task<IObserverRegistrationsStorage> OpenOtherStore() => Task.FromResult<IObserverRegistrationsStorage>(new ObserverRegistrationsStorage(Store(_client.GetDatabase($"{_name}_other")), SerializerOptions));

    static IEventStoreDatabase Store(IMongoDatabase database)
    {
        var store = Substitute.For<IEventStoreDatabase>();
        store.GetCollection<BsonDocument>(Arg.Any<string>()).Returns(call => database.GetCollection<BsonDocument>(call.Arg<string>()));
        return store;
    }
}
