// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer.given;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationEnablement.when_advancing_the_floor;

/// <summary>With the shipped defaults (no proven exclusion, unknown membership) generations cannot be enabled.</summary>
public class and_exclusion_is_not_proven : an_initializer
{
    GenerationFloorAdvanceResult _advanced;

    void Establish()
    {
        _proof = new NotProvenGenerationExclusionProof();
        _membership = new UnknownGenerationMembershipProbe();
        Reset();
        var target = _floor with { MinimumStorageProtocol = new(2), MinimumWriterProtocol = new(2), Revision = 2 };
        _request = new(target, 1, "operator", "advance the floor", new(true, "claimed uniform"));
    }

    GenerationFloorAdvance _request;

    async Task Because()
    {
        _advanced = await new GenerationFloorAdvancement(_storage, _membership).Advance(_name, _request);
        _result = await _initializer.Initialize(_name);
    }

    [Fact] void should_refuse_the_advancement() => _advanced.Value.ShouldBeOfExactType<GenerationFloorMembershipNotUniform>();
    [Fact] void should_not_advance_the_stored_floor() => _store.GenerationFloor.DidNotReceive().Advance(Arg.Any<GenerationFloorAdvance>());
    [Fact] void should_refuse_enablement() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] async Task should_name_exclusion_as_not_proven() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.ExclusionNotProven);
    [Fact] void should_not_adopt_or_bind_anything() => (_catalog.Calls + _catalog.Records.Count + _catalog.Epochs.Count).ShouldEqual(0);
    [Fact] async Task should_have_no_receipts() => (await _audit.GetReceipts()).ShouldBeEmpty();
}
