// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_registering_undefined_delivery_over_a_stream : given.a_registration_stream
{
    void Establish() => _registration.Reactor.Delivery = (ObserverDelivery)42;

    Task Because() => Register();

    [Fact] void should_surface_the_refusal_on_the_stream() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_subscribe_the_reactor() => _reactor.DidNotReceive().SetDefinitionAndSubscribe(Arg.Any<Concepts.Observation.Reactors.ReactorDefinition>());
}
