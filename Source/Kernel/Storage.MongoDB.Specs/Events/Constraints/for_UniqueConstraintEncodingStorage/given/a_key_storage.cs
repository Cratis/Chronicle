// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_UniqueConstraintEncodingStorage.given;

public class a_key_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected const string CollectionName = "unique-constraint-encoding-key";
    protected IUniqueConstraintEncodingStorage _storage;
    protected IMongoDatabase _database;
    MongoClient _client;
    string _name;

    void Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _name = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_name);
        _storage = CreateStorage();
    }

    Task Destroy() => _client.DropDatabaseAsync(_name);

    protected IUniqueConstraintEncodingStorage CreateStorage() => new UniqueConstraintEncodingStorage(_database.GetCollection<BsonDocument>(CollectionName));
}
