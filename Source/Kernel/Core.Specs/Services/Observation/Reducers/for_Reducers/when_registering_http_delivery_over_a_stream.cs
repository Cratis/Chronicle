// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_registering_http_delivery_over_a_stream : given.a_registration_stream
{
    void Establish() => _registration.Reducer.Delivery = ObserverDelivery.Http;

    Task Because() => Register();

    [Fact] void should_surface_the_registration_failure_on_the_stream() => _error.ShouldBeOfExactType<ReducerRegistrationFailed>();
    [Fact] void should_not_subscribe_the_reducer() => _definition.ShouldBeNull();
    [Fact] void should_not_resolve_the_read_model() => _readModel.DidNotReceive().GetDefinition();
}
