// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_private_helper_with_dependencies_returns_an_unclaimed_type : Specification
{
    ReactorScenario<ReactorWithHelper> _scenario;
    Exception _error;

    void Establish()
    {
        _scenario = new ReactorScenario<ReactorWithHelper>();
        _scenario.Services.AddSingleton(Substitute.For<IVibeLookup>());
    }

    async Task Because() => _error = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new VibeStarted("Ada")));

    [Fact] void should_not_register_the_helper_as_a_handler() => _scenario.ShouldNotHaveProduced<Unclaimed>();
    [Fact] void should_not_throw() => _error.ShouldBeNull();

    public interface IVibeLookup;

    public class ReactorWithHelper : IReactor
    {
        Unclaimed Helper(VibeStarted @event, IVibeLookup lookup) => new();
    }

    record Unclaimed;
}
