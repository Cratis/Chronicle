// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationAdmission;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_admitting_a_legacy_store(ReplicaSetMongoDBFixture fixture) : given.a_provider(fixture)
{
    string[] _before;

    async Task Establish()
    {
        await _registry.InsertOneAsync(new(_name));
        _before = await Databases();
    }

    async Task Because()
    {
        await new GenerationFloorAdmission(_storage).Admit(CancellationToken.None);
    }

    [Fact] async Task should_not_upsert_the_system_store() => (await _registry.CountDocumentsAsync(FilterDefinition<EventStore>.Empty)).ShouldEqual(1L);
    [Fact] async Task should_not_create_a_database() => (await Databases()).ShouldEqual(_before);
    [Fact] async Task should_not_create_metadata() => (await _storage.ReadGenerationFloors()).ShouldBeEmpty();
}
