// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Registrations;
using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_registering_without_program_metadata : given.a_registration_stream
{
    Task Because() => Register();

    [Fact] void should_default_to_stream_delivery() => _registration.Reactor.Delivery.ShouldEqual(ObserverDelivery.Stream);
    [Fact] void should_subscribe_with_an_unknown_owner() => _definition.ProgramIdentifier.ShouldEqual(ProgramIdentifier.Unknown);
    [Fact] void should_not_refuse_the_registration() => _error.ShouldBeNull();
}
