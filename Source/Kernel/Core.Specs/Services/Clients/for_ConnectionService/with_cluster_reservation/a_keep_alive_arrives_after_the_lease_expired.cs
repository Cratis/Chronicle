// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class a_keep_alive_arrives_after_the_lease_expired : given.a_two_silo_cluster
{
    Exception _result;
    async Task Establish()
    {
        await Open(_serviceA, AuthenticatedCalls.As("alice"));
        _clock.Advance(TimeSpan.FromHours(1));
    }
    async Task Because() => _result = await Catch.Exception(() => _serviceB.ConnectionKeepAlive(new ConnectionKeepAlive { ConnectionId = Id }, AuthenticatedCalls.As("alice")));

    [Fact] void should_refuse_the_keep_alive() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
