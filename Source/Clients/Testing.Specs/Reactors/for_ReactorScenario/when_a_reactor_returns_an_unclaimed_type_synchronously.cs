// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_returns_an_unclaimed_type_synchronously : Specification
{
    ReactorScenario<UnclaimedReactor> _scenario;
    Exception _error;

    void Establish() => _scenario = new ReactorScenario<UnclaimedReactor>();

    async Task Because() => _error = await Catch.Exception(() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new VibeStarted("Ada")));

    [Fact] void should_reject_the_return_type() => _error.ShouldBeOfExactType<InvalidReactorHandlerReturnType>();

    public class UnclaimedReactor : IReactor
    {
        public Unclaimed Handle(VibeStarted @event) => new();
    }

    public record Unclaimed;
}
