// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationFloorStorage;

public class when_using_a_stale_revision : given.a_floor_storage
{
    GenerationFloorAdvanceResult _result;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because() => _result = await _storage.Advance(Request(_floor with { MinimumWriterProtocol = new(2) }));
    [Fact] void should_conflict() => _result.Value.ShouldBeOfExactType<GenerationFloorRevisionConflict>();
    [Fact] async Task should_not_change_the_floor() => (await _storage.Get()).Revision.ShouldEqual(1UL);
    [Fact] async Task should_not_change_the_audit() => (await _storage.GetHistory()).Count.ShouldEqual(1);
}
