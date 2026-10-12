// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_an_explicit_in_process_event_store : given.a_read_model_scenario<given.NullableReadModelReactor>
{
    void Establish()
    {
        _scenario.Dispose();
        _scenario = new(_defaults, eventStore: _defaults.EventStore);
        _scenario.Services.AddSingleton(_recorder);
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_still_fail_with_the_named_exception() => _exception.ShouldBeOfExactType<ReadModelNotSeededForReactorScenario>();
    [Fact] void should_not_invoke_the_handler() => _recorder.DidNotReceive().Record(Arg.Any<object?>());
}
