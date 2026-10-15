// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_an_unseeded_non_nullable_parameter : given.a_read_model_scenario<given.NonNullableReadModelReactor>
{
    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_fail_with_the_named_exception() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_name_the_read_model_type() => _exception!.Message.Contains(typeof(given.ScenarioReadModel).FullName!, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_resolved_key() => _exception!.Message.Contains("'triggering-source'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_how_to_seed_the_model() => _exception!.Message.Contains("Given.ForEventSourceId(id).ReadModel(...)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_that_given_events_do_not_build_models() => _exception!.Message.Contains("does not build read models from given events", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_invoke_the_handler() => _recorder.DidNotReceive().Record(Arg.Any<object?>());
}
