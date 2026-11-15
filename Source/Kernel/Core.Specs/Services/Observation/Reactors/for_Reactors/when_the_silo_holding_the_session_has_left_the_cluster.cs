// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_the_silo_holding_the_session_has_left_the_cluster : given.a_registration_attempt
{
    Exception _beforeReconnect;
    protected override IConnectionReservation ReservationForCheck() => _held;
    void Establish() => _active.Remove(_siloA);
    async Task Because()
    {
        await Register(AuthenticatedCalls.As("alice"));
        _beforeReconnect = _error;
        await _held.Claim(AuthenticatedCalls.OwnerOf("alice"), _siloB, Guid.NewGuid(), Lease);
        _ownership.Claim("connection", AuthenticatedCalls.OwnerOf("alice"));
        _grainFactory.ClearReceivedCalls();
        await Register(AuthenticatedCalls.As("alice"), expectRegistration: true);
    }

    [Fact] void should_refuse_the_lost_session() => (_beforeReconnect is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_register_after_reconnecting() => _error.ShouldBeNull();
    [Fact] void should_not_check_the_reservation_after_reconnecting_locally() => _grainFactory.DidNotReceive().GetGrain<IConnectionReservation>(Arg.Any<string>(), Arg.Any<string>());
}
