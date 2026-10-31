// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

using Cratis.Chronicle.Storage.MongoDB.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.for_GenerationFloorStorage;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class when_repeating_an_identical_advance(ReplicaSetMongoDBFixture fixture) : given.a_floor_storage(fixture)
{
    GenerationFloorAdvanceResult _result;
    IReadOnlyList<GenerationFloor> _history;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because()
    {
        _result = await _storage.Advance(Request());
        _history = await _storage.GetHistory();
    }
    [Fact] void should_be_idempotent_even_with_the_original_revision() => _result.Value.ShouldBeOfExactType<GenerationFloorAlreadyAtOrAbove>();
    [Fact] void should_not_add_audit_entries() => _history.Count.ShouldEqual(1);
}
