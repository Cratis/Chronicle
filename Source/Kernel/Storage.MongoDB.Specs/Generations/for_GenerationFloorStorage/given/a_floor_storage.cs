// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationFloorStorage.given;

public class a_floor_storage(ReplicaSetMongoDBFixture fixture) : Specification
{
    protected IGenerationFloorStorage _storage;
    protected GenerationFloor _floor = new(new(1), new(1), [GenerationCapability.FloorAdmission], new(Guid.Parse("ef74f125-67aa-4f7c-b365-6c8737a989f0")), 0, DateTimeOffset.MinValue, "", "");
    protected GenerationFloorAdvance Request(GenerationFloor? floor = null, ulong revision = 0) => new(floor ?? _floor, revision, "spec", "explicit request", new(true, "spec members"));
    protected IMongoDatabase _database;
    MongoClient _client;
    string _name;
    async Task Establish()
    {
        _client = new MongoClient(fixture.ConnectionString);
        _name = MongoDBSpecDatabaseNames.New();
        _database = _client.GetDatabase(_name);
        _storage = CreateStorage();
        await Task.CompletedTask;
    }
    Task Destroy() => _client.DropDatabaseAsync(_name);
    protected IGenerationFloorStorage CreateStorage() => new Chronicle.Storage.MongoDB.Generations.GenerationFloorStorage(_database.GetCollection<BsonDocument>("generation-floor"));
}
