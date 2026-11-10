// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_a_reactor_registers_on_another_silo_than_the_session : given.a_registration_attempt
{
    protected override Cratis.Chronicle.Clients.IConnectionReservation ReservationForCheck() => _held;
    async Task Because() => await Register(AuthenticatedCalls.As("alice"), expectRegistration: true);

    [Fact] void should_register() => _error.ShouldBeNull();
    [Fact] void should_not_claim_the_connection_locally() => _ownership.IsOwnedBy("connection", AuthenticatedCalls.OwnerOf("alice")).ShouldBeFalse();
}
