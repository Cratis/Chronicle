// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationAdmission;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_resetting_an_initialized_store(ReplicaSetMongoDBFixture fixture) : given.a_provider(fixture)
{
    string[] _before;
    Exception _error;

    async Task Establish()
    {
        await Advance(1);
        var audit = new Generations.GenerationInitializationStorage(_database.GetEventStoreDatabase(_name).GetCollection<BsonDocument>("generation-initialization"));
        await audit.TryRecord(new(_name, GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow));
        await _client.GetDatabase(_prefix + "other").GetCollection<BsonDocument>("marker").InsertOneAsync(new("kept", true));
        _before = await Databases();
    }

    async Task Because()
    {
        _error = await Catch.Exception(_reset.Reset);
    }

    [Fact] void should_refuse_reset() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_leave_all_databases_intact() => (await Databases()).ShouldEqual(_before);
    [Fact] async Task should_keep_other_data() => (await _client.GetDatabase(_prefix + "other").GetCollection<BsonDocument>("marker").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).ShouldEqual(1L);
    [Fact] async Task should_keep_the_floor() => (await _storage.ReadGenerationFloors())[_name].Revision.ShouldEqual(1UL);
}
