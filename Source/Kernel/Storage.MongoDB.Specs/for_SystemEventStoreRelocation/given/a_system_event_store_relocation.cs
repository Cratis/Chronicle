// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.for_SystemEventStoreRelocation.given;

/// <summary>
/// Base for relocation specs, with every database named under a prefix of its own so specs can list what exists.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB server.</param>
public abstract class a_system_event_store_relocation(MongoDBFixture fixture) : Specification
{
    protected readonly string _prefix = $"rel_{Guid.NewGuid().ToString("N")[..8]}_";
    protected MongoClient _client;
    protected SystemEventStoreRelocation _relocation;

    protected virtual string SystemDatabaseName => "_system";

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>()).Returns(_client);
        var connections = Substitute.For<IMongoDBStorageConnections>();
        connections.GetDatabase(Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => _client.GetDatabase(call.ArgAt<string>(1)));

        _relocation = new(
            connections,
            clientManager,
            Options.Create(new MongoDBOptions { Server = fixture.ConnectionString, Database = "chronicle" }),
            Options.Create(new MongoDBStorageOptions { DatabaseNamePrefix = _prefix, SystemDatabaseName = SystemDatabaseName }),
            NullLogger<SystemEventStoreRelocation>.Instance);
    }

    protected IMongoCollection<BsonDocument> Collection(string database, string collection) =>
        _client.GetDatabase($"{_prefix}{database}").GetCollection<BsonDocument>(collection);

    protected Task Insert(string database, string collection, params BsonDocument[] documents) =>
        Collection(database, collection).InsertManyAsync(documents);

    protected async Task<string[]> Databases()
    {
        var names = await (await _client.ListDatabaseNamesAsync()).ToListAsync();
        return names.Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal))
            .Select(_ => _[_prefix.Length..])
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    async Task Destroy()
    {
        var names = await (await _client.ListDatabaseNamesAsync()).ToListAsync();
        foreach (var name in names.Where(_ => _.StartsWith(_prefix, StringComparison.Ordinal)))
        {
            await _client.DropDatabaseAsync(name);
        }
    }
}
