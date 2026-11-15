// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_legacy_work_appears_during_adoption : given.an_initializer
{
    GenerationInitializationResult _drained;
    int _enabledAfterFirstRun;

    void Establish()
    {
        IReadOnlyList<GenerationInitializationBlocker> late = [new(_name, GenerationInitializationBlockerKind.LegacyReplayJobUnresolved, "job", "started during adoption", DateTimeOffset.UtcNow, null)];
        _legacy.Inspect(_name).Returns(Task.FromResult<IReadOnlyList<GenerationInitializationBlocker>>([]), Task.FromResult(late));
    }

    async Task Because()
    {
        _result = await _initializer.Initialize(_name);
        _enabledAfterFirstRun = (await _audit.GetReceipts()).Count(_ => _.Step == GenerationInitializationStep.Enabled);
        _legacy.Inspect(_name).Returns(Task.FromResult<IReadOnlyList<GenerationInitializationBlocker>>([]));
        _drained = await Create().Initialize(_name);
    }

    [Fact] void should_be_blocked_before_enabling() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] void should_not_record_enabled_while_blocked() => _enabledAfterFirstRun.ShouldEqual(0);
    [Fact] void should_keep_the_adoption() => _catalog.Records.Count.ShouldEqual(2);
    [Fact] void should_enable_once_the_work_is_drained() => _drained.ShouldBeOfExactType<GenerationInitializationEnabled>();
    [Fact] async Task should_not_duplicate_adoption_receipts() => (await _audit.GetReceipts()).Count(_ => _.Step == GenerationInitializationStep.MaterializationAdopted).ShouldEqual(2);
}
