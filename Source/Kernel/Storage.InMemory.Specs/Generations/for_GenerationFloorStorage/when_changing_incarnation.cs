// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationFloorStorage;

public class when_changing_incarnation : given.a_floor_storage
{
    GenerationFloorAdvanceResult _result;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because() => _result = await _storage.Advance(Request(_floor with { Incarnation = new(Guid.NewGuid()) }, 1));
    [Fact] void should_refuse_a_different_incarnation() => _result.Value.ShouldBeOfExactType<GenerationFloorWouldLower>();
    [Fact] async Task should_keep_the_original_incarnation() => (await _storage.Get()).Incarnation.ShouldEqual(_floor.Incarnation);
}
