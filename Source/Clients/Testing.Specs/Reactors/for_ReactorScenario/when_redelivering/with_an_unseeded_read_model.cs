// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_an_unseeded_read_model : a_read_model_scenario<NonNullableReadModelReactor>
{
    RecordedOccurrence _occurrence;

    async Task Establish() => _occurrence = await _scenario.Given.ForEventSource(_source).History(new VibeStarted("Ada"));

    async Task Because() => _exception = await Catch.Exception(() => _scenario.Redeliver(_occurrence));

    [Fact] void should_require_a_seeded_read_model() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_not_invoke_the_handler() => _recorder.DidNotReceiveWithAnyArgs().Record(default);
}
