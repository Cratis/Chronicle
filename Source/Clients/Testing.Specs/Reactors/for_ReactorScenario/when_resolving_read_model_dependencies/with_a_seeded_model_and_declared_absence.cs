// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_a_seeded_model_and_declared_absence : given.a_read_model_scenario<given.NullableReadModelReactor>
{
    given.ScenarioReadModel _seeded;

    void Establish()
    {
        _seeded = new(_source, "Ada");
        _scenario.Given.ForEventSourceId(_source).ReadModel(_seeded).NoReadModel<given.ScenarioReadModel>();
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_not_remove_the_seeded_model() => _recorder.Received(1).Record(_seeded);
}
