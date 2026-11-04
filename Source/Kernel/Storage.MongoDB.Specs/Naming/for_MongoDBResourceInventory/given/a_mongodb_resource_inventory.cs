// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory.given;

public class a_mongodb_resource_inventory : Specification
{
    protected const string DefaultScope = "mongo.local:27017";

    protected IMongoDBStorageConnections _connections;
    protected IClusterStorage _clusterStorage;
    protected IMongoClient _client;
    protected List<IMongoDatabase> _databases;
    protected List<IMongoCollection<MongoDBNamespace>> _collections;
    protected List<string> _registered;
    protected List<string> _physical;
    protected Dictionary<string, string[]> _namespaces;
    protected string _prefix = string.Empty;
    protected Func<StorageConnectionKind, string, string, string, (string Scope, string Database)> _place;
    protected MongoDBResourceInventory _inventory;
    protected IReadOnlyList<ResourceUse> _uses;

    void Establish()
    {
        _registered = [];
        _physical = [];
        _namespaces = [];
        _databases = [];
        _collections = [];
        _place = (_, derived, _, _) => (DefaultScope, derived);

        _client = Substitute.For<IMongoClient>();
        _client.ListDatabaseNamesAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IAsyncCursor<string>>(new ListCursor<string>(_physical)));
        _client.ListDatabaseNamesAsync(Arg.Any<ListDatabaseNamesOptions>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult<IAsyncCursor<string>>(new ListCursor<string>(_physical)));

        _connections = Substitute.For<IMongoDBStorageConnections>();
        _connections.Placement(Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => Task.FromResult(_place(call.Arg<StorageConnectionKind>(), call.ArgAt<string>(1), call.ArgAt<string>(2), call.ArgAt<string>(3))));
        _connections.GetDatabase(Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => DatabaseFor(call.ArgAt<string>(2)));

        _clusterStorage = Substitute.For<IClusterStorage>();
        _clusterStorage.GetEventStores().Returns(_ => Task.FromResult<IEnumerable<EventStoreName>>([.. _registered.Select(name => new EventStoreName(name))]));
    }

    protected async Task Read()
    {
        _inventory = new(
            _connections,
            _clusterStorage,
            Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix }));
        _uses = [.. await _inventory.Read(CancellationToken.None)];
    }

    protected string[] IdentitiesOf(string store, ResourceKind kind = ResourceKind.EventStoreDatabase) =>
        [.. _uses.Where(_ => _.Owner is { Namespace: null } owner && owner.EventStore == store && _.Kind == kind).Select(_ => _.Identity)];

    protected string[] IdentitiesOf(string store, string @namespace, ResourceKind kind) =>
        [.. _uses.Where(_ => _.Owner is { } owner && owner.EventStore == store && owner.Namespace == @namespace && _.Kind == kind).Select(_ => _.Identity)];

    protected string[] Sentinels() => [.. _uses.Where(_ => _.Owner is null).Select(_ => _.Identity)];

    IMongoDatabase DatabaseFor(string eventStore)
    {
        var database = Substitute.For<IMongoDatabase>();
        database.Client.Returns(_client);
        var collection = Substitute.For<IMongoCollection<MongoDBNamespace>>();
        var names = _namespaces.GetValueOrDefault(eventStore) ?? [];
        collection.FindAsync(Arg.Any<FilterDefinition<MongoDBNamespace>>(), Arg.Any<FindOptions<MongoDBNamespace, MongoDBNamespace>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IAsyncCursor<MongoDBNamespace>>(new ListCursor<MongoDBNamespace>(names.Select(name => new MongoDBNamespace { Name = name }).ToList())));
        database.GetCollection<MongoDBNamespace>(Arg.Any<string>(), Arg.Any<MongoCollectionSettings>()).Returns(collection);
        _databases.Add(database);
        _collections.Add(collection);
        return database;
    }
}
