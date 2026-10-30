// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationInitializationStorage.given;

public class an_initialization_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected IGenerationInitializationStorage _audit;

    IMongoDatabase _database;
    MongoClient _client;
    string _name;

    protected GenerationInitializationReceipt Receipt(string evidence = "original") => new(new("orders"), GenerationInitializationStep.FloorVerified, "store", evidence, new(Guid.Parse("93091777-2010-4eba-802b-2dcc773c2d06")), 1, DateTimeOffset.FromUnixTimeSeconds(30000));
    protected GenerationInitializationBlocker Blocker(DateTimeOffset at) => new(new("orders"), GenerationInitializationBlockerKind.ExclusionNotProven, "store", "not proven", at, null);

    void Establish()
    {
        _client = new(fixture.ConnectionString);
        _name = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_name);
        _audit = new Generations.GenerationInitializationStorage(_database.GetCollection<BsonDocument>("generation-initialization"));
    }
    Task Destroy() => _client.DropDatabaseAsync(_name);
}
