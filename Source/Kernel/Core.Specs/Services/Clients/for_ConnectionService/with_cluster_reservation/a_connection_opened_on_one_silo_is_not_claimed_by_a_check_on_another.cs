// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class a_connection_opened_on_one_silo_is_not_claimed_by_a_check_on_another : given.a_two_silo_cluster
{
    Exception _otherCaller;
    async Task Establish() => await Open(_serviceA, AuthenticatedCalls.As("alice"));
    async Task Because()
    {
        await _serviceB.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, AuthenticatedCalls.As("alice"));
        _otherCaller = await Open(_serviceB, AuthenticatedCalls.As("bob"));
    }

    [Fact] void should_not_claim_ownership_on_the_receiving_silo() => Owns(_ownershipB, "alice").ShouldBeFalse();
    [Fact] void should_keep_refusing_other_callers() => ((RpcException)_otherCaller).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
