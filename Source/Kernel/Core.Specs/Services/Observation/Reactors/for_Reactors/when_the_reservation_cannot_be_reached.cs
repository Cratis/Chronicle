// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_the_reservation_cannot_be_reached : given.a_registration_attempt
{
    protected override IConnectionReservation ReservationForCheck()
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.IsHeldBy(Arg.Any<ConnectionOwner>()).Returns<Task<bool>>(_ => throw new NotSupportedException("old silo"));
        return reservation;
    }
    async Task Because() => await Register(AuthenticatedCalls.As("alice"));

    [Fact] void should_report_that_verification_is_unavailable() => (_error is RpcException { StatusCode: StatusCode.Unavailable }).ShouldBeTrue();
    [Fact] void should_explain_the_transient_failure() => ((RpcException)_error).Status.Detail.ShouldEqual("The connection could not be verified.");
    [Fact] void should_not_subscribe() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
