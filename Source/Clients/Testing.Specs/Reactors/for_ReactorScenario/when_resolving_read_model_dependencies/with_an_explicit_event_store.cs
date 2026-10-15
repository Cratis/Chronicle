// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_resolving_read_model_dependencies;

public class with_an_explicit_event_store : given.a_read_model_scenario<given.NullableReadModelReactor>
{
    void Establish()
    {
        var eventStore = Substitute.For<IEventStore>();
#pragma warning disable CA2263 // The resolver calls the runtime-type overloads.
        eventStore.Projections.HasFor(typeof(given.ScenarioReadModel)).Returns(true);
        eventStore.ReadModels.GetInstanceById(typeof(given.ScenarioReadModel), Arg.Any<ReadModelKey>())
            .Returns(Task.FromResult<object>(null!));
#pragma warning restore CA2263
        _scenario.Dispose();
        _scenario = new(_defaults, eventStore: eventStore);
        _scenario.Services.AddSingleton(_recorder);
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_source).Events(new VibeStarted("Ada")));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_pass_null_to_the_handler() => _recorder.Received(1).Record(null);
}
