// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Storage.Generations;
using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationAdmission;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_resetting_before_admission(ReplicaSetMongoDBFixture fixture) : given.a_provider(fixture)
{
    Exception _error;

    async Task Establish()
    {
        await Advance(2);
        await _reset.Reset();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => new GenerationFloorAdmission(_storage).Admit(CancellationToken.None));
    }

    [Fact] void should_refuse_the_below_floor_binary() => _error.ShouldBeOfExactType<KernelBelowGenerationFloor>();
    [Fact] async Task should_preserve_the_floor() => (await _storage.ReadGenerationFloors())[_name].Revision.ShouldEqual(1UL);
    [Fact] async Task should_not_upsert_the_system_store() => (await _registry.CountDocumentsAsync(FilterDefinition<EventStore>.Empty)).ShouldEqual(1L);
}
