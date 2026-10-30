// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Generations.Bindings.for_MaterializationKeys;

public class when_the_floor_is_not_established : given.a_materialization
{
    Exception _error;
    void Establish() => _store.GenerationFloor.Get().Returns(Task.FromResult<Cratis.Chronicle.Concepts.Projections.Generations.Enablement.GenerationFloor?>(null));
    async Task Because() => _error = await Catch.Exception(async () => await _keys.For(_name, _namespace, _projection));
    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GenerationFloorNotEstablished>();
    [Fact] void should_not_read_definitions() => _store.Projections.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.Concepts.Projections.ProjectionId>());
}
