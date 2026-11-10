// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_the_reservation_does_not_answer : given.a_registration_attempt
{
    protected override IConnectionReservation ReservationForCheck()
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.IsHeldBy(AuthenticatedCalls.OwnerOf("alice")).Returns(new TaskCompletionSource<bool>().Task);
        return reservation;
    }
    async Task Because() => await Register(AuthenticatedCalls.As("alice"));

    [Fact] void should_report_that_verification_is_unavailable() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_not_subscribe() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
