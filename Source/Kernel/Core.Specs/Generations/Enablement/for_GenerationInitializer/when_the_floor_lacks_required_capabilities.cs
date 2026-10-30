// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_the_floor_lacks_required_capabilities : given.an_initializer
{
    void Establish() => _store.GenerationFloor.Get().Returns(Task.FromResult<GenerationFloor?>(new GenerationFloor(_floor.MinimumStorageProtocol, _floor.MinimumWriterProtocol, [GenerationCapability.FloorAdmission], _floor.Incarnation, 1, _floor.AdvancedAt, _floor.AdvancedBy, _floor.Reason)));

    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] async Task should_record_a_floor_blocker() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.FloorNotEstablished);
    [Fact] void should_not_adopt() => _catalog.Records.ShouldBeEmpty();
}
