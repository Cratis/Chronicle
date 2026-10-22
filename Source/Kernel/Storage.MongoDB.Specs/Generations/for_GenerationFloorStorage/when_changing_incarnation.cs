// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationFloorStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_changing_incarnation(ReplicaSetMongoDBFixture fixture) : given.a_floor_storage(fixture)
{
    GenerationFloorAdvanceResult _result;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because() => _result = await _storage.Advance(Request(_floor with { Incarnation = new(Guid.NewGuid()) }, 1));
    [Fact] void should_refuse_a_different_incarnation() => _result.Value.ShouldBeOfExactType<GenerationFloorWouldLower>();
    [Fact] async Task should_keep_the_original_incarnation() => (await _storage.Get()).Incarnation.ShouldEqual(_floor.Incarnation);
}
