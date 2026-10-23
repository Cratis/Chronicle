// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Admission.for_GenerationFloorAdvancement;

public class when_membership_is_unknown : Specification
{
    IStorage _storage;
    GenerationFloorAdvanceResult _result;
    void Establish() => _storage = Substitute.For<IStorage>();
    async Task Because()
    {
        var floor = new GenerationFloor(new(1), new(1), [], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", "");
        var service = new GenerationFloorAdvancement(_storage, new UnknownGenerationMembershipProbe());
        _result = await service.Advance("orders", new(floor, 0, "operator", "request", new(true, "operator attestation is not proof")));
    }
    [Fact] void should_refuse_attestation_when_probe_is_unknown() => _result.Value.ShouldBeOfExactType<GenerationFloorMembershipNotUniform>();
    [Fact] void should_not_even_open_the_store_for_writing() => _storage.DidNotReceiveWithAnyArgs().GetEventStore(Arg.Any<EventStoreName>());
}
