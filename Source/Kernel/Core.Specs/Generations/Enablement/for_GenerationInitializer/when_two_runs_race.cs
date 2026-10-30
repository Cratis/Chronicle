// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_two_runs_race : given.an_initializer
{
    GenerationInitializationResult[] _results;

    async Task Because() => _results = await Task.WhenAll(Task.Run(() => Create().Initialize(_name)), Task.Run(() => Create().Initialize(_name)));
    [Fact] void should_reserve_once_per_subject() => _catalog.Records.Count.ShouldEqual(2);
    [Fact] void should_bind_once_per_subject() => _catalog.Epochs.Count.ShouldEqual(2);
    [Fact] async Task should_keep_one_enabled_marker() => (await _audit.GetReceipts()).Count(_ => _.Step == GenerationInitializationStep.Enabled).ShouldEqual(1);
    [Fact] void should_not_refuse_identical_work() => _results.Any(_ => _ is GenerationInitializationBlocked).ShouldBeFalse();
}
