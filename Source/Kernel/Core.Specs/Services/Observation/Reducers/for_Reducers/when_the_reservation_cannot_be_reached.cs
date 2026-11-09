// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_the_reservation_cannot_be_reached : given.a_registration_attempt
{
    protected override IConnectionReservation ReservationForCheck()
    {
        var reservation = Substitute.For<IConnectionReservation>();
        reservation.IsHeldBy(AuthenticatedCalls.OwnerOf("alice")).Returns<Task<bool>>(_ => throw new InvalidOperationException("offline"));
        return reservation;
    }
    async Task Because() => await Register(AuthenticatedCalls.As("alice"));

    [Fact] void should_report_that_verification_is_unavailable() => ((RpcException)_error).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_not_resolve_the_read_model() => _definitionGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_subscribe() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
