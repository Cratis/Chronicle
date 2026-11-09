// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_the_lease_has_expired_on_another_silo : given.a_registration_attempt
{
    protected override IConnectionReservation ReservationForCheck() => _held;
    void Establish() => _clock.Advance(Lease);
    async Task Because() => await Register(AuthenticatedCalls.As("alice"));

    [Fact] void should_refuse_the_registration() => (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_subscribe() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
