// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_deliberate_absence_before_on_stream : given.a_read_model_scenario<given.NullableReadModelReactor>
{
    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSourceId(_source)
            .NoReadModel<given.ScenarioReadModel>().OnStream("Vibe", "Reminders", "morning")
            .Events(new VibeStarted("Ada")));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_pass_null_to_the_handler() => _recorder.Received(1).Record(null);
}
