// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBKernelStateResetHandler;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_a_generation_floor_is_present(ReplicaSetMongoDBFixture fixture) : Specification
{
    MongoClient _client;
    string _prefix;
    string _name;
    Chronicle.Storage.MongoDB.Generations.GenerationFloorStorage _floor;
    MongoDBKernelStateResetHandler _handler;
    ulong _revision;
    int _history;
    string[] _collections;
    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _prefix = $"floor_{Guid.NewGuid():N}_";
        _name = _prefix + "orders+es";
        var database = _client.GetDatabase(_name);
        _floor = new(database.GetCollection<BsonDocument>("generation-floor"));
        await _floor.Advance(new(new(new(1), new(1), [], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", ""), 0, "spec", "test", new(true, "spec")));
        await database.GetCollection<BsonDocument>("payload").InsertOneAsync(new BsonDocument("value", 1));
        var clients = Substitute.For<IMongoDBClientManager>();
        clients.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        _handler = new(Options.Create(new ChronicleOptions()), Options.Create(new MongoDBOptions { Server = fixture.ConnectionString }), clients, Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix }));
    }
    async Task Because()
    {
        await _handler.Reset();
        _revision = (await _floor.Get()).Revision;
        _history = (await _floor.GetHistory()).Count;
        using var cursor = await _client.GetDatabase(_name).ListCollectionNamesAsync();
        _collections = (await cursor.ToListAsync()).ToArray();
    }
    [Fact] void should_keep_the_floor() => _revision.ShouldEqual(1UL);
    [Fact] void should_keep_the_audit() => _history.ShouldEqual(1);
    [Fact] void should_reset_other_collections() => _collections.ShouldEqual(["generation-floor"]);
    Task Destroy() => _client.DropDatabaseAsync(_name);
}
