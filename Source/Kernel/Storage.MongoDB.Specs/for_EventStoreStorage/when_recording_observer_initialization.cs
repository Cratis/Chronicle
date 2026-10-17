// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_recording_observer_initialization(ReplicaSetMongoDBFixture fixture) : Specification
{
    IMongoClient _client;
    string _databaseName;
    IEventStoreDatabase _database;
    EventStoreStorage _storage;
    bool _before;
    bool _after;
    bool _setup;
    long _markers;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        var database = _client.GetDatabase(_databaseName);
        _database = Substitute.For<IEventStoreDatabase>();
        _database.GetCollection<BsonDocument>(Arg.Any<string>()).Returns(call => database.GetCollection<BsonDocument>(call.Arg<string>()));
        _storage = CreateStorage();
        await _storage.MarkInitialized();
        _before = await _storage.AreObserversInitialized();
    }

    async Task Because()
    {
        await _storage.MarkObserversInitialized();
        await _storage.MarkObserversInitialized();
        _after = await CreateStorage().AreObserversInitialized();
        _setup = await CreateStorage().IsInitialized();
        _markers = await _database.GetCollection<BsonDocument>("initialization").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_not_treat_store_setup_as_observer_initialization() => _before.ShouldBeFalse();
    [Fact] void should_keep_observer_initialization_across_storage_instances() => _after.ShouldBeTrue();
    [Fact] void should_keep_store_setup() => _setup.ShouldBeTrue();
    [Fact] void should_keep_one_marker_for_each_purpose() => _markers.ShouldEqual(2L);

    Task Destroy() => _client.DropDatabaseAsync(_databaseName);

    EventStoreStorage CreateStorage() => new(
        "orders",
        _database,
        Substitute.For<Json.IExpandoObjectConverter>(),
        new JsonSerializerOptions(),
        _ => new Chronicle.Storage.Sinks.Sinks("orders", "default", new KnownInstancesOf<ISinkFactory>([])),
        Substitute.For<Orleans.Storage.IJobsStorage>(),
        Options.Create(new ChronicleOptions()),
        NullLoggerFactory.Instance);
}
