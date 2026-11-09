// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class a_keep_alive_arrives_on_another_silo_than_the_session : given.a_two_silo_cluster
{
    Exception _result;
    protected override bool RouteKeepAlivesAcrossSilos => true;

    async Task Establish() => await Open(_serviceA, AuthenticatedCalls.As("alice"));

    async Task Because() => _result = await Catch.Exception(() => _serviceB.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, AuthenticatedCalls.As("alice")));

    [Fact] void should_be_served() => _result.ShouldBeNull();
    [Fact] async Task should_ping_the_owning_registry() => await _registries[_siloA.ToParsableString()].Received(1).OnClientPing(new(Id));
    [Fact] void should_not_claim_the_connection_on_the_receiving_silo() => Owns(_ownershipB, "alice").ShouldBeFalse();
}
