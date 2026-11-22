// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_does_not_answer : given.a_two_silo_cluster
{
    Exception _result;

    protected override IConnectionReservation Decorate(IConnectionReservation grain)
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.Claim(Arg.Any<ConnectionOwner>(), Arg.Any<SiloAddress>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>()).Returns(new TaskCompletionSource<bool>().Task);
        return reservation;
    }

    async Task Because() => _result = await Open(_serviceA, AuthenticatedCalls.As("alice"));

    [Fact] void should_serve_the_client_without_waiting_for_the_reservation() => _result.ShouldBeNull();
    [Fact] void should_hold_the_connection_locally() => Owns(_ownershipA, "alice").ShouldBeTrue();
}
