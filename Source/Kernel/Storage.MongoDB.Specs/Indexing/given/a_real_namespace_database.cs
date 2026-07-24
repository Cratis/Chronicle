// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Indexing.given;

/// <summary>
/// Base for index specs that need a real MongoDB-backed <see cref="EventStoreNamespaceDatabase"/>.
/// </summary>
/// <param name="fixture">The shared <see cref="MongoDBFixture"/> providing a MongoDB container.</param>
public abstract class a_real_namespace_database(MongoDBFixture fixture) : Specification
{
    protected EventStoreNamespaceDatabase _database;
    protected IMongoDatabase _rawDatabase;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;
    string _databaseName = default!;

    static a_real_namespace_database() => BsonSerializer.RegisterSerializationProvider(new ConceptSerializationProvider());

    void Establish()
    {
        _eventStore = new EventStoreName($"idx_{Guid.NewGuid():N}");
        _namespace = new EventStoreNamespaceName("default");
        _databaseName = $"{_eventStore}+es+{_namespace}";

        var clientManager = Substitute.For<IMongoDBClientManager>();
        clientManager.GetClientFor(Arg.Any<MongoClientSettings>())
            .Returns(callInfo => new MongoClient(callInfo.Arg<MongoClientSettings>()));

        var options = Options.Create(new MongoDBOptions
        {
            Server = fixture.ConnectionString,
            Database = "chronicle"
        });

        _database = new EventStoreNamespaceDatabase(_eventStore, _namespace, clientManager, options);
        _rawDatabase = new MongoClient(fixture.ConnectionString).GetDatabase(_databaseName);
    }

    protected async Task<IReadOnlyList<string>> IndexNamesFor(string collectionName)
    {
        var collection = _rawDatabase.GetCollection<BsonDocument>(collectionName);
        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        return indexes.ConvertAll(index => index["name"].AsString);
    }

    protected async Task<BsonDocument> IndexFor(string collectionName, string indexName)
    {
        var collection = _rawDatabase.GetCollection<BsonDocument>(collectionName);
        using var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();
        return indexes.First(index => index["name"].AsString == indexName);
    }

    async Task Destroy() => await new MongoClient(fixture.ConnectionString).DropDatabaseAsync(_databaseName);
}
