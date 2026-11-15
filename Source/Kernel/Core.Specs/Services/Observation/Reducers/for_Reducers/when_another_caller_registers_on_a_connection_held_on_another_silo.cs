// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_another_caller_registers_on_a_connection_held_on_another_silo : given.a_registration_attempt
{
    protected override IConnectionReservation ReservationForCheck() => _held;
    void Establish() => _ownership.Claim("bobs-connection", AuthenticatedCalls.OwnerOf("bob"));
    async Task Because() => await Register(AuthenticatedCalls.As("bob"));

    [Fact] void should_refuse_the_registration() => (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_resolve_the_read_model() => _definitionGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_subscribe() => _mediator.ReceivedCalls().ShouldBeEmpty();
}
