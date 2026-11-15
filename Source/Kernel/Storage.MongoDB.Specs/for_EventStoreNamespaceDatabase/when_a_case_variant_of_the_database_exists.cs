// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceDatabase;

/// <summary>
/// A namespace stored as 'cratis' next to an existing 'Studio+es+Cratis' database: MongoDB refuses to create a
/// database differing only in case, so the namespace must open the existing one, both through connection resolution
/// and without it.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class when_a_case_variant_of_the_database_exists(MongoDBFixture fixture) : Specification
{
    readonly string _prefix = $"nscase_{Guid.NewGuid().ToString("N")[..8]}_";
    MongoClient _client;
    EventStoreNamespaceDatabase _direct;
    EventStoreNamespaceDatabase _throughConnections;
    long _directEvents;
    long _connectionEvents;
    string[] _databases;
    bool _variantHasOwnData;
    bool _exactHasOwnData;

    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        await _client.GetDatabase($"{_prefix}Studio+es+Cratis").GetCollection<BsonDocument>(WellKnownCollectionNames.EventLog)
            .InsertManyAsync([new BsonDocument("_id", 0L), new BsonDocument("_id", 1L)]);

        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        var mongoOptions = Options.Create(new MongoDBOptions { Server = fixture.ConnectionString, Database = "chronicle" });
        var storageOptions = Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix });
        var connections = new MongoDBStorageConnections(
            mongoOptions,
            new StorageConnectionResolution(WellKnownConnectionStorage.MongoDB, new ConnectionResolutionOptions(), Substitute.For<IConnectionResolverFactory>()),
            clientManager);

        _direct = new("Studio", "cratis", clientManager, mongoOptions, storageOptions);
        _throughConnections = new("Studio", "cratis", clientManager, mongoOptions, storageOptions, connections);
    }

    async Task Because()
    {
        _variantHasOwnData = await _direct.HasOwnDatabase();
        _exactHasOwnData = await new EventStoreNamespaceDatabase(
            "Studio",
            "Cratis",
            CreateClientManager(),
            Options.Create(new MongoDBOptions { Server = fixture.ConnectionString }),
            Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix })).HasOwnDatabase();
        await _direct.EnsureIndexesForEventSequence(EventSequenceId.Log);
        await _throughConnections.EnsureIndexesForEventSequence(EventSequenceId.Log);
        _directEvents = await _direct.GetEventSequenceCollectionAsBsonFor(EventSequenceId.Log).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _connectionEvents = await _throughConnections.GetEventSequenceCollectionAsBsonFor(EventSequenceId.Log).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        _databases = [.. (await (await _client.ListDatabaseNamesAsync()).ToListAsync()).Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal))];
    }

    [Fact] void should_read_the_existing_events_without_connection_resolution() => _directEvents.ShouldEqual(2L);
    [Fact] void should_read_the_existing_events_through_connection_resolution() => _connectionEvents.ShouldEqual(2L);
    [Fact] void should_keep_only_the_existing_database() => _databases.ShouldContainOnly($"{_prefix}Studio+es+Cratis");

    [Fact] void should_not_treat_the_variant_as_having_its_own_data() => _variantHasOwnData.ShouldBeFalse();
    [Fact] void should_find_data_under_the_exact_name() => _exactHasOwnData.ShouldBeTrue();

    IMongoDBClientManager CreateClientManager()
    {
        var manager = Substitute.For<IMongoDBClientManager>();
        manager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        return manager;
    }

    async Task Destroy()
    {
        foreach (var name in await (await _client.ListDatabaseNamesAsync()).ToListAsync())
        {
            if (name.StartsWith(_prefix, StringComparison.Ordinal))
            {
                await _client.DropDatabaseAsync(name);
            }
        }
    }
}
