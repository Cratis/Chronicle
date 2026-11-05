// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Observation.Reactors.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_a_reactor_registers_on_a_connection_an_administrator_does_not_hold : given.a_registration_attempt
{
    void Establish()
    {
        _ownership.Claim("connection", AuthenticatedCalls.OwnerOf("alice"));
    }

    async Task Because() => await Register(AuthenticatedCalls.As("admin", administrator: true));

    [Fact] void should_refuse_the_registration() => (_error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_not_subscribe_to_the_mediator() => _mediator.DidNotReceive().Subscribe(Arg.Any<ReactorId>(), Arg.Any<ConnectionId>(), Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReactorEventsObserver>());
    [Fact] void should_not_reach_a_grain() => _grainFactory.ReceivedCalls().ShouldBeEmpty();
}
