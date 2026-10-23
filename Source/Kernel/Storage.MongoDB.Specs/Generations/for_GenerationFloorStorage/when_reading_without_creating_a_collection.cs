// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationFloorStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_reading_without_creating_a_collection(ReplicaSetMongoDBFixture fixture) : given.a_floor_storage(fixture)
{
    string[] _collections;
    async Task Because()
    {
        await _storage.Get();
        await _storage.GetHistory();
        using var cursor = await _database.ListCollectionNamesAsync();
        _collections = (await cursor.ToListAsync()).ToArray();
    }
    [Fact] void should_not_create_generation_storage() => _collections.ShouldBeEmpty();
}
