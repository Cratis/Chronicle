// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_a_reducer_registers_on_another_silo_than_the_session : given.a_registration_attempt
{
    protected override Cratis.Chronicle.Clients.IConnectionReservation ReservationForCheck() => _held;
    async Task Because() => await Register(AuthenticatedCalls.As("alice"), expectRegistration: true);

    [Fact] void should_register() => _error.ShouldBeNull();
    [Fact] async Task should_resolve_the_read_model() => await _definitionGrain.Received(1).GetDefinition();
    [Fact] void should_not_claim_the_connection_locally() => _ownership.IsOwnedBy("connection", AuthenticatedCalls.OwnerOf("alice")).ShouldBeFalse();
}
