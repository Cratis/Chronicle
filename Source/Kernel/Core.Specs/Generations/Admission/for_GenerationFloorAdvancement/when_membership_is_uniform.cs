// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Generations.Admission.for_GenerationFloorAdvancement;

public class when_membership_is_uniform : Specification
{
    IStorage _storage;
    IGenerationMembershipProbe _probe;
    GenerationFloorAdvance _request;
    GenerationFloorAdvanceResult _result;
    GenerationFloorAdvanceResult _expected;
    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _probe = Substitute.For<IGenerationMembershipProbe>();
        var floor = new GenerationFloor(new(1), new(1), [], new(Guid.NewGuid()), 0, DateTimeOffset.MinValue, "", "");
        _request = new(floor, 0, "operator", "request", new(false, "stale"));
        _probe.Probe(floor).Returns(new GenerationMembershipEvidence(true, "fresh probe"));
        _expected = new GenerationFloorAdvanced(floor);
        _storage.GetEventStore("orders").GenerationFloor.Advance(Arg.Any<GenerationFloorAdvance>()).Returns(_expected);
    }
    async Task Because() => _result = await new GenerationFloorAdvancement(_storage, _probe).Advance("orders", _request);
    [Fact] void should_forward_the_storage_outcome() => _result.ShouldBeSame(_expected);
    [Fact] void should_use_fresh_evidence() => _storage.GetEventStore("orders").GenerationFloor.Received(1).Advance(Arg.Is<GenerationFloorAdvance>(_ => _.Evidence.Reason == "fresh probe"));
}
