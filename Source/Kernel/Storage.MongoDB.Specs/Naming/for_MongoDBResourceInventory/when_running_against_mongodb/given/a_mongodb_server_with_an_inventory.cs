// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_MongoDBResourceInventory.when_running_against_mongodb.given;

public abstract class a_mongodb_server_with_an_inventory(MongoDBFixture fixture) : Specification
{
    protected readonly string _prefix = $"inv_{Guid.NewGuid().ToString("N")[..8]}_";
    protected MongoClient _client;
    IMongoDBStorageConnections _connections;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _connections = Substitute.For<IMongoDBStorageConnections>();
        _connections.Placement(Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => Task.FromResult(("spec-server", call.ArgAt<string>(1))));
        _connections.GetDatabase(Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => _client.GetDatabase(call.ArgAt<string>(1)));
    }

    protected async Task<Exception?> TryCreate(string eventStore)
    {
        try
        {
            await _client.GetDatabase($"{_prefix}{eventStore}+es")
                .GetCollection<BsonDocument>("namespaces")
                .InsertOneAsync(new BsonDocument("_id", "Default"));
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    protected async Task<string[]> Databases() =>
        [.. (await (await _client.ListDatabaseNamesAsync()).ToListAsync()).Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal))];

    protected async Task<IReadOnlyList<ResourceUse>> Read(params string[] eventStores)
    {
        var cluster = Substitute.For<IClusterStorage>();
        cluster.GetEventStores().Returns(_ => Task.FromResult<IEnumerable<EventStoreName>>([.. eventStores.Select(name => new EventStoreName(name))]));
        var inventory = new MongoDBResourceInventory(_connections, cluster, Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix }));
        return [.. await inventory.Read(CancellationToken.None)];
    }

    protected static string[] IdentitiesOf(IEnumerable<ResourceUse> uses, string eventStore) =>
        [.. uses.Where(_ => _.Owner is { Namespace: null } owner && owner.EventStore == eventStore && _.Kind == ResourceKind.EventStoreDatabase).Select(_ => _.Identity)];

    async Task Destroy()
    {
        foreach (var name in await Databases())
        {
            await _client.DropDatabaseAsync(name);
        }
    }
}
