// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Chronicle.Clients;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_a_different_identity_rides_an_owned_connection_id : given.a_registration_attempt
{
    protected override IConnectionReservation ReservationForCheck() => _held;
    void Establish() => _ownership.Claim("bobs-connection", AuthenticatedCalls.OwnerOf("bob"));

    [Fact] async Task should_refuse_a_caller_who_holds_a_different_connection() => await Refuse(AuthenticatedCalls.As("bob"));
    [Fact] async Task should_refuse_a_client_with_the_same_subject() => await Refuse(AuthenticatedCalls.AsClient("alice"));
    [Fact] async Task should_refuse_an_anonymous_caller() => await Refuse(AuthenticatedCalls.Unauthenticated());
    [Fact] async Task should_refuse_an_in_process_caller() => await Refuse(default);

    [Fact]
    async Task should_refuse_the_same_subject_from_another_issuer()
    {
        var context = AuthenticatedCalls.As("alice");
        context.ServerCallContext.GetHttpContext().User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice", ClaimValueTypes.String, "other-issuer")], "test"));
        await Refuse(context);
    }

    async Task Refuse(CallContext context)
    {
        await Register(context);
        (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
        _mediator.ReceivedCalls().ShouldBeEmpty();
    }
}
