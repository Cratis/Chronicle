// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_SinkProvenanceStorage.given;

public class a_provenance_database(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected MongoClient _client;
    protected IMongoDatabase _database;
    protected string _databaseName;
    protected SinkProvenanceStorage _storage;
    protected IMongoCollection<BsonDocument> _metadata;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _databaseName = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_databaseName);
        _storage = new(_database);
        _metadata = _database.GetCollection<BsonDocument>(SinkProvenanceNames.Metadata);
    }

    async Task Destroy()
    {
        await _client.DropDatabaseAsync(_databaseName);
        await _client.DropDatabaseAsync($"{_databaseName}_independent");
    }
}
