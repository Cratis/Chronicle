// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_an_unseeded_passive_model : given.a_read_model_scenario<given.PassiveReadModelReactor>
{
    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_fail_with_the_named_exception() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_not_build_the_passive_model_from_given_events() => _recorder.DidNotReceive().Record(Arg.Any<object?>());
}
