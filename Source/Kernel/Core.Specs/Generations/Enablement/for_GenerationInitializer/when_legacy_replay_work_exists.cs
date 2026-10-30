// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_legacy_replay_work_exists : given.an_initializer
{
    void Establish() => _legacy.Inspect(_name).Returns([new GenerationInitializationBlocker(_name, GenerationInitializationBlockerKind.LegacyReplaySessionUnresolved, "session", "exists", DateTimeOffset.UtcNow, null), new GenerationInitializationBlocker(_name, GenerationInitializationBlockerKind.LegacyReplayJobUnresolved, "job", "running", DateTimeOffset.UtcNow, null)]);
    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] async Task should_record_every_blocker() => (await _audit.GetBlockers()).Count.ShouldEqual(2);
    [Fact] void should_not_adopt() => _catalog.Records.ShouldBeEmpty();
    [Fact] async Task should_not_record_reconciled() => (await _audit.GetReceipts()).Any(_ => _.Step == GenerationInitializationStep.LegacyWorkReconciled).ShouldBeFalse();
}
