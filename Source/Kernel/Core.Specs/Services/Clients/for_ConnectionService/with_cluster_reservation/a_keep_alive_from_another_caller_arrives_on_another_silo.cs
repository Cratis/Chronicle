// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class a_keep_alive_from_another_caller_arrives_on_another_silo : given.a_two_silo_cluster
{
    Exception _result;
    async Task Establish() => await Open(_serviceA, AuthenticatedCalls.As("alice"));
    async Task Because() => _result = await Catch.Exception(() => _serviceB.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, AuthenticatedCalls.As("bob")));

    [Fact] async Task should_not_ping_the_owning_registry() => await _registries[_siloA.ToParsableString()].DidNotReceive().OnClientPing(Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>());
    [Fact] void should_refuse_the_keep_alive() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_claim_the_connection() => Owns(_ownershipB, "bob").ShouldBeFalse();
}
