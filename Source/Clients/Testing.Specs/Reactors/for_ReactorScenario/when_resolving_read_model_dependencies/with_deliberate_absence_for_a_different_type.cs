// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_deliberate_absence_for_a_different_type : given.a_read_model_scenario<given.NullableReadModelReactor>
{
    void Establish() => _scenario.Given.ForEventSourceId(_source).NoReadModel<VibeAttendees>();

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_fail_with_the_named_exception() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_not_invoke_the_handler() => _recorder.DidNotReceive().Record(Arg.Any<object?>());
}
