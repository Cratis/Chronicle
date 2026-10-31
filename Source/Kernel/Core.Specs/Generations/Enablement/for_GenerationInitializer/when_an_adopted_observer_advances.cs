// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_an_adopted_observer_advances : given.an_initializer
{
    GenerationInitializationResult _blocked;

    async Task Establish()
    {
        _catalog.Conflict = new("projection-b");
        _blocked = await _initializer.Initialize(_name);
        _catalog.Conflict = null;
        _snapshot[0] = _snapshot[0] with { PositionEvidence = "84", FailedPartitions = 0 };
    }

    async Task Because() => _result = await Create().Initialize(_name);

    [Fact] void should_have_blocked_the_first_run() => _blocked.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] void should_enable_on_resume() => _result.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] void should_bind_both_materializations() => _catalog.Epochs.Count.ShouldEqual(2);
    [Fact] async Task should_resolve_the_conflict() => (await _audit.GetBlockers()).Single().ResolvedAt.ShouldNotBeNull();
    [Fact] async Task should_keep_the_original_position_as_information() => (await _audit.GetReceipts()).Single(receipt => receipt.Step == GenerationInitializationStep.MaterializationAdopted && receipt.Subject.EndsWith("/projection-a", StringComparison.Ordinal)).Evidence.ShouldContain("Position=42;FailedPartitions=2");
}
