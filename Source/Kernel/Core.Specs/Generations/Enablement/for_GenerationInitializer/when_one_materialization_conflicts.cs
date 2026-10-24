// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_one_materialization_conflicts : given.an_initializer
{
    void Establish() => _catalog.Conflict = new("projection-b");

    async Task Because() => _result = await _initializer.Initialize(_name);
    [Fact] void should_not_bind_any_subject() => _catalog.Epochs.ShouldBeEmpty();
    [Fact] async Task should_record_conflict() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.ContradictoryLegacyState);
    [Fact] async Task should_not_enable() => (await _audit.GetReceipts()).Any(_ => _.Step == GenerationInitializationStep.Enabled).ShouldBeFalse();
}
