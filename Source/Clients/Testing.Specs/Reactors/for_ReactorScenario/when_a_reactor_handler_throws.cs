// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

public class when_a_reactor_handler_throws : Specification
{
    ReactorScenario<ThrowingReactor> _scenario;
    EventSourceId _id;
    Exception _exception;

    void Establish()
    {
        _id = EventSourceId.New();
        _scenario = new ReactorScenario<ThrowingReactor>();
    }

    async Task Because() =>
        _exception = await Catch.Exception(() => _scenario.Given.ForEventSource(_id).Events(new ReservationMade("Ada")));

    [Fact] void should_surface_what_the_handler_threw() => _exception.ShouldBeOfExactType<ReservationNotYetVisible>();
}
