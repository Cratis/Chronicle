// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_with_dependencies_returns_an_unclaimed_type_synchronously : Specification
{
    ReactorScenario<UnclaimedReactor> _scenario;
    Exception _error;

    void Establish()
    {
        _scenario = new ReactorScenario<UnclaimedReactor>();
        _scenario.Services.AddSingleton(Substitute.For<IVibeLookup>());
    }

    async Task Because() => _error = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new VibeStarted("Ada")));

    [Fact] void should_reject_the_return_type() => _error.ShouldBeOfExactType<InvalidReactorHandlerReturnType>();

    public interface IVibeLookup;

    public class UnclaimedReactor : IReactor
    {
        public Task Handle(VibeStarted @event) => Task.CompletedTask;

        public Unclaimed Other(VibeStarted @event, IVibeLookup lookup) => new();
    }

    public record Unclaimed;
}
