// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_the_connection_belongs_to_another_caller : given.a_registration_lifetime
{
    void Establish() => _ownership.Claim(Arg.Any<ConnectionId>(), Arg.Any<ConnectionOwner>()).Returns(ConnectionClaim.Refused);

    async Task Because()
    {
        Connect();
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_report_a_terminal_notification() => _terminalArrived.ShouldBeTrue();
    [Fact] void should_refuse_with_permission_denied() => ((RpcException)_errors.Single()).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_register_the_client() => Registrations.ShouldEqual(0);
    [Fact] void should_not_send_a_heartbeat() => _heartbeats.ShouldEqual(0);
    [Fact] void should_not_disconnect_the_existing_connection() => Disconnects.ShouldEqual(0);
    [Fact] void should_not_release_a_claim_it_does_not_hold() => _ownership.DidNotReceive().Release(Arg.Any<ConnectionId>(), Arg.Any<ConnectionOwner>());
}
