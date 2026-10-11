// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_models_seeded_under_other_keys : given.a_read_model_scenario<given.NonNullableReadModelReactor>
{
    void Establish()
    {
        _scenario.Given.ForEventSourceId("first-seeded-key").ReadModel(new given.ScenarioReadModel("first-seeded-key", "Ada"));
        _scenario.Given.ForEventSourceId("second-seeded-key").ReadModel(new given.ScenarioReadModel("second-seeded-key", "Grace"));
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_fail_with_the_named_exception() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_list_the_first_seeded_key() => _exception!.Message.Contains("'first-seeded-key'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_list_the_second_seeded_key() => _exception!.Message.Contains("'second-seeded-key'", StringComparison.Ordinal).ShouldBeTrue();
}
