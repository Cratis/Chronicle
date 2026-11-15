// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class another_caller_opens_the_connection_within_the_retention : given.a_two_silo_cluster
{
    Exception _result;

    async Task Establish()
    {
        using var cancellation = new CancellationTokenSource();
        await Open(_serviceA, AuthenticatedCalls.As("alice", cancellationToken: cancellation.Token));
        await cancellation.CancelAsync();
        await Released();
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention - TimeSpan.FromMinutes(1));
    }

    async Task Because() => _result = await Open(_serviceB, AuthenticatedCalls.As("bob"));

    [Fact] void should_be_refused() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
}
