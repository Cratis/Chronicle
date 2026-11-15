// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_connecting;

public class and_the_connection_is_registered_on_another_silo : given.a_registration_lifetime
{
    void Establish()
    {
        var other = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11112), 2);
        var remote = Substitute.For<IConnectedClients>();
        remote.IsConnected(Arg.Any<ConnectionId>()).Returns(true);
        _grainFactory.GetGrain<IConnectedClients>(other.ToParsableString(), Arg.Any<string>()).Returns(remote);
        AllowingConnectionOwnership.WithActiveSilos(_grainFactory, _localSilo, other);
    }

    async Task Because()
    {
        Connect();
        _terminalArrived = await Arrives(_terminal.Task);
    }

    [Fact] void should_refuse_with_permission_denied() => ((RpcException)_errors.Single()).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_register_the_client() => Registrations.ShouldEqual(0);
    [Fact] void should_not_claim_the_connection() => _ownership.DidNotReceive().Claim(Arg.Any<ConnectionId>(), Arg.Any<ConnectionOwner>());
}
